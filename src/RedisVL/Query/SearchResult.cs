namespace RedisVL.Query;

/// <summary>
/// Result from a search query.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The document ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The document fields.
    /// </summary>
    public Dictionary<string, object?> Fields { get; set; } = new();
    
    /// <summary>
    /// The search score (distance for vector, BM25 for text).
    /// </summary>
    public double? Score { get; set; }
    
    /// <summary>
    /// Gets a field value as a specific type.
    /// </summary>
    public T? GetField<T>(string name)
    {
        if (Fields.TryGetValue(name, out var value) && value != null)
        {
            if (value is T typedValue)
                return typedValue;
            
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }
}

/// <summary>
/// Collection of search results.
/// </summary>
public class SearchResults
{
    /// <summary>
    /// Total number of matching documents.
    /// </summary>
    public long TotalResults { get; set; }
    
    /// <summary>
    /// The documents returned.
    /// </summary>
    public List<SearchResult> Documents { get; set; } = new();
    
    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public double? ExecutionTimeMs { get; set; }
}
