using System.Globalization;

namespace RedisVL.Query;

/// <summary>
/// Vector range query - finds vectors within a specified distance.
/// </summary>
public class RangeQuery : BaseQuery
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
    /// Maximum distance threshold.
    /// </summary>
    public double DistanceThreshold { get; set; } = 0.2;
    
    /// <summary>
    /// Name for the score field in results.
    /// </summary>
    public string ScoreFieldName { get; set; } = "vector_distance";
    
    public RangeQuery() { }
    
    public RangeQuery(float[] vector, string vectorFieldName, double distanceThreshold)
    {
        Vector = vector;
        VectorFieldName = vectorFieldName;
        DistanceThreshold = distanceThreshold;
    }
    
    public override string GetQueryString()
    {
        var filter = GetFilterString();
        return $"({filter} @{VectorFieldName}:[VECTOR_RANGE {DistanceThreshold.ToString(CultureInfo.InvariantCulture)} $vec_param])";
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
