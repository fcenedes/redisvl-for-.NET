using System.Text.Json;
using RedisVL.Index;
using RedisVL.Query;
using RedisVL.Query.Filter;
using RedisVL.Schema;

namespace RedisVL.Extensions.MessageHistory;

/// <summary>
/// Base class for LLM message history using Redis.
/// Supports storing, retrieving by recency, and clearing conversation history.
/// </summary>
public class BaseMessageHistory : IDisposable
{
    protected readonly SearchIndex Index;
    protected readonly string SessionName;
    private bool _initialized;
    private long _messageCounter;
    
    /// <summary>
    /// Creates a base message history.
    /// </summary>
    /// <param name="name">Session name (used as index name).</param>
    /// <param name="redisUrl">Redis connection URL.</param>
    /// <param name="prefix">Key prefix for messages.</param>
    public BaseMessageHistory(
        string name,
        string redisUrl = "redis://localhost:6379",
        string? prefix = null)
    {
        SessionName = name;
        var schema = BuildSchema(name, prefix ?? $"message:{name}");
        Index = new SearchIndex(schema, redisUrl);
    }
    
    /// <summary>
    /// Adds messages to the history.
    /// </summary>
    public virtual async Task AddMessagesAsync(IEnumerable<Message> messages)
    {
        await EnsureInitializedAsync();
        
        var data = new List<Dictionary<string, object>>();
        
        foreach (var msg in messages)
        {
            _messageCounter++;
            var entry = new Dictionary<string, object>
            {
                ["role"] = msg.Role,
                ["content"] = msg.Content,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["order"] = _messageCounter,
                ["session"] = SessionName
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
    /// Adds a single message to the history.
    /// </summary>
    public Task AddMessageAsync(Message message) => AddMessagesAsync(new[] { message });
    
    /// <summary>
    /// Gets recent messages from the history.
    /// </summary>
    /// <param name="topK">Number of recent messages to return.</param>
    /// <param name="role">Optional single role filter.</param>
    /// <returns>Messages in chronological order.</returns>
    public virtual Task<IList<Message>> GetRecentAsync(int topK = 5, string? role = null)
    {
        string[]? roles = role != null ? new[] { role } : null;
        return GetRecentAsync(topK, roles);
    }

    /// <summary>
    /// Gets recent messages from the history, filtered by one or more roles.
    /// </summary>
    /// <param name="topK">Number of recent messages to return.</param>
    /// <param name="roles">Optional role filter (multiple roles produce a Tag IN filter).</param>
    /// <returns>Messages in chronological order.</returns>
    public virtual async Task<IList<Message>> GetRecentAsync(int topK, string[]? roles)
    {
        await EnsureInitializedAsync();

        FilterExpression? filter = null;
        if (roles != null && roles.Length > 0)
        {
            if (roles.Length == 1)
            {
                filter = Tag.Field("role") == roles[0];
            }
            else
            {
                filter = Tag.Field("role").In(roles);
            }
        }

        var query = new FilterQuery
        {
            FilterExpression = filter,
            NumResults = topK,
            SortBy = "order",
            SortAscending = false,
            ReturnFields = new[] { "role", "content", "metadata", "timestamp" }
        };

        var results = await Index.QueryAsync(query);

        var messages = results.Documents.Select(doc => new Message
        {
            Role = doc.GetField<string>("role") ?? string.Empty,
            Content = doc.GetField<string>("content") ?? string.Empty,
            Metadata = doc.Fields.ContainsKey("metadata") && doc.Fields["metadata"] != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(doc.GetField<string>("metadata")!)
                : null
        }).Reverse().ToList();

        return messages;
    }
    
    /// <summary>
    /// Clears all messages from the history.
    /// </summary>
    public virtual async Task ClearAsync()
    {
        try
        {
            await Index.DropAsync(dropDocuments: true);
            _initialized = false;
            _messageCounter = 0;
        }
        catch
        {
            // Ignore if index doesn't exist
        }
    }
    
    /// <summary>
    /// Gets the total number of messages in the history.
    /// </summary>
    public async Task<long> CountAsync()
    {
        await EnsureInitializedAsync();
        return await Index.CountAsync();
    }
    
    protected async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await Index.CreateAsync(overwrite: false);
            _initialized = true;
        }
    }
    
    protected virtual IndexSchema BuildSchema(string name, string prefix)
    {
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
                new { name = "session", type = "tag" }
            }
        });
        return IndexSchema.FromJson(json);
    }
    
    public virtual void Dispose()
    {
        Index.Dispose();
        GC.SuppressFinalize(this);
    }
}
