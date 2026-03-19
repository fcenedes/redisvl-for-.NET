using System.Text.Json;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;
using RedisVL.Schema;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Extensions.Router;

/// <summary>
/// Semantic router that classifies queries into predefined routes
/// using vector similarity search in Redis.
/// </summary>
public class SemanticRouter : IDisposable
{
    private readonly SearchIndex _index;
    private readonly ITextVectorizer _vectorizer;
    private readonly IList<Route> _routes;
    private readonly string _name;
    private bool _initialized;
    
    /// <summary>
    /// Creates a semantic router.
    /// </summary>
    /// <param name="name">Router name (used as index name).</param>
    /// <param name="routes">Route definitions.</param>
    /// <param name="vectorizer">Text vectorizer for embedding queries and references.</param>
    /// <param name="redisUrl">Redis connection URL.</param>
    /// <param name="prefix">Key prefix for route entries.</param>
    public SemanticRouter(
        string name,
        IList<Route> routes,
        ITextVectorizer vectorizer,
        string redisUrl = "redis://localhost:6379",
        string? prefix = null)
    {
        _name = name;
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _vectorizer = vectorizer ?? throw new ArgumentNullException(nameof(vectorizer));
        
        var schema = BuildSchema(name, prefix ?? $"router:{name}", vectorizer.Dims);
        _index = new SearchIndex(schema, redisUrl);
    }
    
    /// <summary>
    /// Routes a query to the best matching route.
    /// </summary>
    /// <param name="query">The query text to route.</param>
    /// <returns>The best matching route, or null if no route matches.</returns>
    public async Task<RouteMatch?> RouteAsync(string query)
    {
        await EnsureInitializedAsync();
        
        var embedding = await _vectorizer.EmbedAsync(query, "search_query");
        
        // Search across all routes
        var vectorQuery = new VectorQuery(embedding, "embedding", _routes.Count * 10)
        {
            ReturnFields = new[] { "route_name", "reference", "metadata", "distance_threshold", "vector_distance" }
        };
        
        var results = await _index.QueryAsync(vectorQuery);
        
        // Find the best match that meets the route's distance threshold
        foreach (var doc in results.Documents.OrderBy(d => d.Score ?? double.MaxValue))
        {
            var routeName = doc.GetField<string>("route_name");
            var distance = doc.Score ?? double.MaxValue;
            
            // Get the route's distance threshold
            var thresholdStr = doc.GetField<string>("distance_threshold");
            var threshold = double.TryParse(thresholdStr, out var t) ? t : 0.5;
            
            if (distance <= threshold)
            {
                var metadataStr = doc.GetField<string>("metadata");
                return new RouteMatch
                {
                    Name = routeName ?? string.Empty,
                    Distance = distance,
                    MatchedReference = doc.GetField<string>("reference"),
                    Metadata = !string.IsNullOrEmpty(metadataStr)
                        ? JsonSerializer.Deserialize<Dictionary<string, string>>(metadataStr)
                        : null
                };
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Routes a query (operator-style shorthand for RouteAsync).
    /// </summary>
    public Task<RouteMatch?> InvokeAsync(string query) => RouteAsync(query);
    
    /// <summary>
    /// Clears all route data from Redis.
    /// </summary>
    public async Task ClearAsync()
    {
        try
        {
            await _index.DropAsync(dropDocuments: true);
            _initialized = false;
        }
        catch
        {
            // Ignore if index doesn't exist
        }
    }
    
    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        
        await _index.CreateAsync(overwrite: true);
        
        // Load all route references
        var allTexts = new List<string>();
        var allData = new List<Dictionary<string, object>>();
        
        foreach (var route in _routes)
        {
            foreach (var reference in route.References)
            {
                allTexts.Add(reference);
                allData.Add(new Dictionary<string, object>
                {
                    ["route_name"] = route.Name,
                    ["reference"] = reference,
                    ["distance_threshold"] = route.DistanceThreshold,
                    ["metadata"] = route.Metadata != null ? JsonSerializer.Serialize(route.Metadata) : ""
                });
            }
        }
        
        // Batch embed all references
        var embeddings = await _vectorizer.EmbedManyAsync(allTexts, "search_document");
        
        for (int i = 0; i < allData.Count; i++)
        {
            allData[i]["embedding"] = embeddings[i];
        }
        
        await _index.LoadAsync(allData);
        _initialized = true;
    }
    
    private static IndexSchema BuildSchema(string name, string prefix, int dims)
    {
        var json = JsonSerializer.Serialize(new
        {
            index = new { name, prefix, storage_type = "hash" },
            fields = new object[]
            {
                new { name = "route_name", type = "tag" },
                new { name = "reference", type = "text" },
                new { name = "distance_threshold", type = "numeric" },
                new { name = "metadata", type = "text", attrs = new { no_index = true } },
                new { name = "embedding", type = "vector", attrs = new { 
                    algorithm = "hnsw", dims = dims > 0 ? dims : 1536, distance_metric = "cosine", datatype = "FLOAT32" 
                }}
            }
        });
        return IndexSchema.FromJson(json);
    }
    
    public void Dispose()
    {
        _index.Dispose();
        GC.SuppressFinalize(this);
    }
}
