using NRedisStack.Search;

namespace RedisVL.Schema.Fields;

/// <summary>
/// Text field for full-text search.
/// </summary>
public class TextField : FieldBase
{
    public double Weight { get; set; } = 1.0;
    public bool NoStem { get; set; }
    public string? Phonetic { get; set; }
    
    public override FieldName ToRedisField()
    {
        var field = FieldName.Of(Name);
        return field;
    }
}
