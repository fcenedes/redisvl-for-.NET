using RedisVL.Query;
using RedisVL.Schema;

namespace RedisVL.Index;

/// <summary>
/// Interface for Redis search index operations.
/// </summary>
public interface ISearchIndex
{
    /// <summary>
    /// Gets the index schema.
    /// </summary>
    IndexSchema Schema { get; }
    
    /// <summary>
    /// Gets the index name.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Creates the index in Redis.
    /// </summary>
    Task CreateAsync(bool overwrite = false);
    
    /// <summary>
    /// Deletes the index from Redis.
    /// </summary>
    /// <param name="dropDocuments">If true, also deletes all indexed documents.</param>
    Task DropAsync(bool dropDocuments = false);
    
    /// <summary>
    /// Checks if the index exists in Redis.
    /// </summary>
    Task<bool> ExistsAsync();
    
    /// <summary>
    /// Gets information about the index.
    /// </summary>
    Task<Dictionary<string, object>> InfoAsync();
    
    /// <summary>
    /// Loads data into the index.
    /// </summary>
    /// <param name="data">List of documents as dictionaries.</param>
    /// <param name="idField">Field name to use as the document ID.</param>
    /// <param name="keys">Explicit keys (overrides idField).</param>
    /// <param name="ttl">Optional TTL for the documents.</param>
    Task LoadAsync(IEnumerable<Dictionary<string, object>> data, string? idField = null, 
        IEnumerable<string>? keys = null, TimeSpan? ttl = null);
    
    /// <summary>
    /// Fetches a document by ID.
    /// </summary>
    Task<Dictionary<string, object>?> FetchAsync(string id);
    
    /// <summary>
    /// Deletes documents by their IDs.
    /// </summary>
    Task DeleteAsync(IEnumerable<string> ids);
    
    /// <summary>
    /// Queries the index.
    /// </summary>
    Task<SearchResults> QueryAsync(BaseQuery query);
    
    /// <summary>
    /// Counts documents matching a query.
    /// </summary>
    Task<long> CountAsync(CountQuery? query = null);
}
