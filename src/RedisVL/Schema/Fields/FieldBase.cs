using NRedisStack.Search;

namespace RedisVL.Schema.Fields;

/// <summary>
/// Distance metric for vector similarity search.
/// </summary>
public enum DistanceMetric
{
    Cosine,
    L2,
    IP // Inner Product
}

/// <summary>
/// Vector indexing algorithm.
/// </summary>
public enum VectorAlgorithm
{
    Flat,
    HNSW
}

/// <summary>
/// Storage type for Redis data.
/// </summary>
public enum StorageType
{
    Hash,
    Json
}

/// <summary>
/// Base class for all field definitions.
/// </summary>
public abstract class FieldBase
{
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public bool Sortable { get; set; }
    public bool NoIndex { get; set; }
    
    /// <summary>
    /// Gets the field path for JSON documents or name for Hash.
    /// </summary>
    public string FieldPath => Path ?? (Name.StartsWith("$.") ? Name : $"$.{Name}");
    
    /// <summary>
    /// Converts this field definition to a RediSearch schema field.
    /// </summary>
    public abstract FieldName ToRedisField();
}
