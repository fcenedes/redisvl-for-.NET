namespace RedisVL.Tutorial.Services;

/// <summary>
/// Available vectorizer modes for the tutorial application.
/// </summary>
public enum VectorizerMode
{
    /// <summary>
    /// Demo mode using SHA256 hash-based vectorizer. Works offline, no API key needed.
    /// Only matches exact text — not true semantic similarity.
    /// </summary>
    Demo,

    /// <summary>
    /// OpenAI mode using text-embedding-3-small (1536 dims).
    /// Requires an OpenAI API key for true semantic similarity.
    /// </summary>
    OpenAI,

    /// <summary>
    /// HuggingFace mode using sentence-transformers/all-MiniLM-L6-v2 (384 dims).
    /// Requires a HuggingFace API token for true semantic similarity.
    /// </summary>
    HuggingFace
}

