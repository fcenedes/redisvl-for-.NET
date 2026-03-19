namespace RedisVL.Utils.Rerankers;

/// <summary>
/// Result from a reranking operation.
/// </summary>
public class RerankResult
{
    /// <summary>
    /// Original index in the input list.
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Relevance score from the reranker.
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// The original content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Interface for reranking search results.
/// </summary>
public interface IReranker
{
    /// <summary>
    /// Gets the model name used for reranking.
    /// </summary>
    string Model { get; }
    
    /// <summary>
    /// Reranks a list of documents based on relevance to a query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="documents">Documents to rerank.</param>
    /// <param name="topK">Number of top results to return (0 = all).</param>
    /// <returns>Reranked results sorted by relevance.</returns>
    Task<IList<RerankResult>> RerankAsync(string query, IList<string> documents, int topK = 0);
}
