using NRedisStack.Search;

namespace RedisVL.Schema.Fields;

/// <summary>
/// Tag field for exact match filtering.
/// </summary>
public class TagField : FieldBase
{
    public string Separator { get; set; } = ",";
    public bool CaseSensitive { get; set; }
    
    public override FieldName ToRedisField()
    {
        var field = FieldName.Of(Name);
        return field;
    }
}
