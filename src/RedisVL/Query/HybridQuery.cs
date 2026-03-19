namespace RedisVL.Query;

/// <summary>
/// Combination method for hybrid search.
/// </summary>
public enum HybridCombinationMethod
{
    /// <summary>
    /// Linear combination of scores.
    /// </summary>
    Linear,
    
    /// <summary>
    /// Reciprocal Rank Fusion.
    /// </summary>
    RRF
}

/// <summary>
/// Hybrid query combining vector and text search (Redis 8.4.0+).
/// </summary>
public class HybridQuery : BaseQuery
{
    /// <summary>
    /// The query vector.
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Name of the vector field.
    /// </summary>
    public string VectorFieldName { get; set; } = "embedding";
    
    /// <summary>
    /// The text to search for.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the text field.
    /// </summary>
    public string TextFieldName { get; set; } = "content";
    
    /// <summary>
    /// How to combine vector and text scores.
    /// </summary>
    public HybridCombinationMethod CombinationMethod { get; set; } = HybridCombinationMethod.Linear;
    
    /// <summary>
    /// Weight for vector score (0-1) when using Linear combination.
    /// </summary>
    public double VectorWeight { get; set; } = 0.5;
    
    /// <summary>
    /// Weight for text score (0-1) when using Linear combination.
    /// </summary>
    public double TextWeight { get; set; } = 0.5;
    
    /// <summary>
    /// Name for the score field in results.
    /// </summary>
    public string ScoreFieldName { get; set; } = "hybrid_score";
    
    public HybridQuery() { }
    
    public HybridQuery(float[] vector, string vectorFieldName, string text, string textFieldName)
    {
        Vector = vector;
        VectorFieldName = vectorFieldName;
        Text = text;
        TextFieldName = textFieldName;
    }
    
    public override string GetQueryString()
    {
        var filter = GetFilterString();
        var method = CombinationMethod == HybridCombinationMethod.RRF ? "RRF" : "LINEAR";
        
        // Hybrid query format for Redis 8.4+
        return $"({filter})=>[KNN {NumResults} @{VectorFieldName} $vec_param HYBRID @{TextFieldName}:({Text}) {method} AS {ScoreFieldName}]";
    }
    
    /// <summary>
    /// Gets the vector as a byte array for Redis.
    /// </summary>
    public byte[] GetVectorBytes()
    {
        var bytes = new byte[Vector.Length * sizeof(float)];
        Buffer.BlockCopy(Vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
