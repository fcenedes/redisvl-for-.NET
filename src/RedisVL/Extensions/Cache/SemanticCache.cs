using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RedisVL.Exceptions;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;
using RedisVL.Schema;
using RedisVL.Schema.Fields;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Extensions.Cache;

/// <summary>
/// A cached LLM response entry.
/// </summary>
public class CacheEntry
{
    /// <summary>
    /// The original prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    
    /// <summary>
    /// The cached response.
    /// </summary>
    public string Response { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// The semantic distance from the query.
    /// </summary>
    public double? Distance { get; set; }
}

/// <summary>
/// Semantic cache for LLM responses using vector similarity.
/// Reduces LLM costs by returning cached responses for semantically similar prompts.
/// </summary>
public class SemanticCache : IDisposable
{
    private readonly SearchIndex _index;
    private readonly ITextVectorizer _vectorizer;
    private readonly double _distanceThreshold;
    private readonly TimeSpan? _ttl;
    private readonly string _name;
    private bool _initialized;
    
    /// <summary>
    /// Creates a semantic cache.
    /// </summary>
    /// <param name="name">Cache name (used as index name).</param>
    /// <param name="vectorizer">Text vectorizer for embedding prompts.</param>
    /// <param name="redisUrl">Redis connection URL.</param>
    /// <param name="distanceThreshold">Maximum distance for a cache hit (lower = stricter). Default: 0.1</param>
    /// <param name="ttl">Time-to-live for cache entries.</param>
    /// <param name="prefix">Key prefix for cached entries.</param>
    public SemanticCache(
        string name,
        ITextVectorizer vectorizer,
        string redisUrl = "redis://localhost:6379",
        double distanceThreshold = 0.1,
        TimeSpan? ttl = null,
        string? prefix = null)
    {
        _name = name;
        _vectorizer = vectorizer ?? throw new ArgumentNullException(nameof(vectorizer));
        _distanceThreshold = distanceThreshold;
        _ttl = ttl;
        
        var schema = BuildSchema(name, prefix ?? $"llmcache:{name}", vectorizer.Dims);
        _index = new SearchIndex(schema, redisUrl);
    }
    
    /// <summary>
    /// Stores a prompt-response pair in the cache.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="response">The LLM response.</param>
    /// <param name="metadata">Optional metadata to store with the entry.</param>
    public async Task StoreAsync(string prompt, string response, Dictionary<string, string>? metadata = null)
    {
        await EnsureInitializedAsync();
        
        var embedding = await _vectorizer.EmbedAsync(prompt, "search_document");
        var promptHash = ComputeHash(prompt);
        
        var data = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["response"] = response,
            ["prompt_hash"] = promptHash,
            ["embedding"] = embedding,
            ["created_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        if (metadata != null)
        {
            data["metadata"] = JsonSerializer.Serialize(metadata);
        }
        
        await _index.LoadAsync(new[] { data }, keys: new[] { promptHash }, ttl: _ttl);
    }
    
    /// <summary>
    /// Checks the cache for a semantically similar prompt.
    /// </summary>
    /// <param name="prompt">The prompt to check.</param>
    /// <param name="numResults">Maximum number of results to return.</param>
    /// <returns>List of matching cache entries, or empty if no match.</returns>
    public async Task<IList<CacheEntry>> CheckAsync(string prompt, int numResults = 1)
    {
        await EnsureInitializedAsync();
        
        var embedding = await _vectorizer.EmbedAsync(prompt, "search_query");
        
        var query = new RangeQuery(embedding, "embedding", _distanceThreshold)
        {
            ReturnFields = new[] { "prompt", "response", "metadata", "vector_distance" },
            NumResults = numResults
        };
        
        var results = await _index.QueryAsync(query);
        
        return results.Documents.Select(doc => new CacheEntry
        {
            Prompt = doc.GetField<string>("prompt") ?? string.Empty,
            Response = doc.GetField<string>("response") ?? string.Empty,
            Metadata = doc.Fields.ContainsKey("metadata") 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(doc.GetField<string>("metadata")!) 
                : null,
            Distance = doc.Score
        }).ToList();
    }
    
    /// <summary>
    /// Clears all entries from the cache.
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
        if (!_initialized)
        {
            await _index.CreateAsync(overwrite: false);
            _initialized = true;
        }
    }
    
    private static IndexSchema BuildSchema(string name, string prefix, int dims)
    {
        var json = JsonSerializer.Serialize(new
        {
            index = new { name, prefix, storage_type = "hash" },
            fields = new object[]
            {
                new { name = "prompt", type = "text" },
                new { name = "response", type = "text", attrs = new { no_index = true } },
                new { name = "prompt_hash", type = "tag" },
                new { name = "metadata", type = "text", attrs = new { no_index = true } },
                new { name = "created_at", type = "numeric", attrs = new { sortable = true } },
                new { name = "embedding", type = "vector", attrs = new { 
                    algorithm = "hnsw", dims = dims > 0 ? dims : 1536, distance_metric = "cosine", datatype = "FLOAT32" 
                }}
            }
        });
        return IndexSchema.FromJson(json);
    }
    
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
    
    public void Dispose()
    {
        _index.Dispose();
        GC.SuppressFinalize(this);
    }
}
