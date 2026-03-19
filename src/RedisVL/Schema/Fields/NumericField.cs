using NRedisStack.Search;

namespace RedisVL.Schema.Fields;

/// <summary>
/// Numeric field for range queries.
/// </summary>
public class NumericField : FieldBase
{
    public override FieldName ToRedisField()
    {
        var field = FieldName.Of(Name);
        return field;
    }
}
