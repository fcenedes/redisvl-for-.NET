using NRedisStack.Search;

namespace RedisVL.Schema.Fields;

/// <summary>
/// Vector field for similarity search.
/// </summary>
public class VectorField : FieldBase
{
    public VectorAlgorithm Algorithm { get; set; } = VectorAlgorithm.HNSW;
    public int Dims { get; set; }
    public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.Cosine;
    public string DataType { get; set; } = "FLOAT32";
    
    // HNSW specific parameters
    public int M { get; set; } = 16;
    public int EfConstruction { get; set; } = 200;
    public int EfRuntime { get; set; } = 10;
    
    // FLAT specific parameters
    public int BlockSize { get; set; } = 1024;
    
    public override FieldName ToRedisField()
    {
        var field = FieldName.Of(Name);
        return field;
    }
    
    /// <summary>
    /// Gets the Redis distance metric string.
    /// </summary>
    public string GetDistanceMetricString() => DistanceMetric switch
    {
        DistanceMetric.Cosine => "COSINE",
        DistanceMetric.L2 => "L2",
        DistanceMetric.IP => "IP",
        _ => "COSINE"
    };
    
    /// <summary>
    /// Gets the Redis algorithm string.
    /// </summary>
    public string GetAlgorithmString() => Algorithm switch
    {
        VectorAlgorithm.Flat => "FLAT",
        VectorAlgorithm.HNSW => "HNSW",
        _ => "HNSW"
    };
}
