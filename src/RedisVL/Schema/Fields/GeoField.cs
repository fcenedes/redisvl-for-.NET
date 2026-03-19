using NRedisStack.Search;

namespace RedisVL.Schema.Fields;

/// <summary>
/// Geographic field for location-based queries.
/// </summary>
public class GeoField : FieldBase
{
    public override FieldName ToRedisField()
    {
        var field = FieldName.Of(Name);
        return field;
    }
}
