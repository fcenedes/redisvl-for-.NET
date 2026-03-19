using RedisVL.Query.Filter;

namespace RedisVL.Query;

/// <summary>
/// Vector similarity search query using KNN.
/// </summary>
public class VectorQuery : BaseQuery
{
    /// <summary>
    /// The query vector.
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Name of the vector field to search.
    /// </summary>
    public string VectorFieldName { get; set; } = "embedding";
    
    /// <summary>
    /// Return the distance/score in results.
    /// </summary>
    public bool ReturnScore { get; set; } = true;
    
    /// <summary>
    /// Name for the score field in results.
    /// </summary>
    public string ScoreFieldName { get; set; } = "vector_distance";
    
    /// <summary>
    /// EF runtime parameter for HNSW (optional).
    /// </summary>
    public int? EfRuntime { get; set; }
    
    public VectorQuery() { }
    
    public VectorQuery(float[] vector, string vectorFieldName, int numResults = 10)
    {
        Vector = vector;
        VectorFieldName = vectorFieldName;
        NumResults = numResults;
    }
    
    public override string GetQueryString()
    {
        var filter = GetFilterString();
        // KNN query format: (filter)=>[KNN num_results @field $vec_param AS score]
        var efParam = EfRuntime.HasValue ? $" EF_RUNTIME {EfRuntime.Value}" : "";
        return $"({filter})=>[KNN {NumResults} @{VectorFieldName} $vec_param{efParam} AS {ScoreFieldName}]";
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
