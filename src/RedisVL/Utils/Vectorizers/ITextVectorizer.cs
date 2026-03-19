namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Interface for text vectorization (embedding) providers.
/// </summary>
public interface ITextVectorizer
{
    /// <summary>
    /// Gets the model name used for vectorization.
    /// </summary>
    string Model { get; }
    
    /// <summary>
    /// Gets the embedding dimensions.
    /// </summary>
    int Dims { get; }
    
    /// <summary>
    /// Embeds a single text string into a vector.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="inputType">Optional input type hint (e.g., "search_query", "search_document").</param>
    /// <returns>The embedding vector.</returns>
    Task<float[]> EmbedAsync(string text, string? inputType = null);
    
    /// <summary>
    /// Embeds multiple text strings into vectors.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="inputType">Optional input type hint.</param>
    /// <returns>List of embedding vectors.</returns>
    Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null);
}
