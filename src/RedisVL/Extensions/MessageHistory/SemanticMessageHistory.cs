using System.Text.Json;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;
using RedisVL.Schema;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Extensions.MessageHistory;

/// <summary>
/// Message history with semantic search capability.
/// Extends base message history with the ability to retrieve relevant messages
/// based on semantic similarity using vector search.
/// </summary>
public class SemanticMessageHistory : BaseMessageHistory
{
    private readonly ITextVectorizer _vectorizer;
    private readonly double _distanceThreshold;
    
    /// <summary>
    /// Creates a semantic message history.
    /// </summary>
    /// <param name="name">Session name.</param>
    /// <param name="vectorizer">Text vectorizer for embedding messages.</param>
    /// <param name="redisUrl">Redis connection URL.</param>
    /// <param name="distanceThreshold">Distance threshold for semantic retrieval.</param>
    /// <param name="prefix">Key prefix for messages.</param>
    public SemanticMessageHistory(
        string name,
        ITextVectorizer vectorizer,
        string redisUrl = "redis://localhost:6379",
        double distanceThreshold = 0.7,
        string? prefix = null) 
        : base(name, redisUrl, prefix)
    {
        _vectorizer = vectorizer ?? throw new ArgumentNullException(nameof(vectorizer));
        _distanceThreshold = distanceThreshold;
    }
    
    /// <inheritdoc />
    public override async Task AddMessagesAsync(IEnumerable<Message> messages)
    {
        await EnsureInitializedAsync();
        
        var data = new List<Dictionary<string, object>>();
        var messageList = messages.ToList();
        
        // Embed all messages
        var texts = messageList.Select(m => m.Content).ToList();
        var embeddings = await _vectorizer.EmbedManyAsync(texts, "search_document");
        
        for (int i = 0; i < messageList.Count; i++)
        {
            var msg = messageList[i];
            var entry = new Dictionary<string, object>
            {
                ["role"] = msg.Role,
                ["content"] = msg.Content,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["order"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + i,
                ["session"] = SessionName,
                ["embedding"] = embeddings[i]
            };
            
            if (msg.Metadata != null)
            {
                entry["metadata"] = JsonSerializer.Serialize(msg.Metadata);
            }
            
            data.Add(entry);
        }
        
        await Index.LoadAsync(data);
    }
    
    /// <summary>
    /// Gets messages relevant to a query using semantic similarity.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Number of relevant messages to return.</param>
    /// <param name="role">Optional role filter.</param>
    /// <returns>Messages ranked by relevance.</returns>
    public async Task<IList<Message>> GetRelevantAsync(string query, int topK = 5, string? role = null)
    {
        await EnsureInitializedAsync();
        
        var embedding = await _vectorizer.EmbedAsync(query, "search_query");
        
        var vectorQuery = new VectorQuery(embedding, "embedding", topK)
        {
            ReturnFields = new[] { "role", "content", "metadata", "timestamp", "vector_distance" }
        };
        
        if (!string.IsNullOrEmpty(role))
        {
            vectorQuery.FilterExpression = Tag.Field("role") == role;
        }
        
        var results = await Index.QueryAsync(vectorQuery);
        
        return results.Documents
            .Where(doc => doc.Score == null || doc.Score <= _distanceThreshold)
            .Select(doc => new Message
            {
                Role = doc.GetField<string>("role") ?? string.Empty,
                Content = doc.GetField<string>("content") ?? string.Empty,
                Metadata = doc.Fields.ContainsKey("metadata") && doc.Fields["metadata"] != null
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(doc.GetField<string>("metadata")!)
                    : null
            }).ToList();
    }
    
    /// <inheritdoc />
    protected override IndexSchema BuildSchema(string name, string prefix)
    {
        var dims = _vectorizer?.Dims > 0 ? _vectorizer.Dims : 1536;
        var json = JsonSerializer.Serialize(new
        {
            index = new { name, prefix, storage_type = "hash" },
            fields = new object[]
            {
                new { name = "role", type = "tag" },
                new { name = "content", type = "text" },
                new { name = "metadata", type = "text", attrs = new { no_index = true } },
                new { name = "timestamp", type = "numeric", attrs = new { sortable = true } },
                new { name = "order", type = "numeric", attrs = new { sortable = true } },
                new { name = "session", type = "tag" },
                new { name = "embedding", type = "vector", attrs = new { 
                    algorithm = "hnsw", dims, distance_metric = "cosine", datatype = "FLOAT32" 
                }}
            }
        });
        return IndexSchema.FromJson(json);
    }
}
