namespace RedisVL.Query.Filter;

/// <summary>
/// Builder for Tag field filters.
/// </summary>
public class Tag
{
    private readonly string _fieldName;
    
    private Tag(string fieldName)
    {
        _fieldName = fieldName;
    }
    
    /// <summary>
    /// Creates a tag filter builder for the specified field.
    /// </summary>
    public static Tag Field(string fieldName) => new(fieldName);
    
    /// <summary>
    /// Equals filter - matches exact tag value.
    /// </summary>
    public static FilterExpression operator ==(Tag tag, string value)
        => new TagEqualsFilter(tag._fieldName, value);
    
    /// <summary>
    /// Not equals filter.
    /// </summary>
    public static FilterExpression operator !=(Tag tag, string value)
        => new TagNotEqualsFilter(tag._fieldName, value);
    
    /// <summary>
    /// Matches any of the specified values.
    /// </summary>
    public TagInFilter In(params string[] values) => new(_fieldName, values);
    
    public override bool Equals(object? obj) => obj is Tag other && _fieldName == other._fieldName;
    public override int GetHashCode() => _fieldName.GetHashCode();
}

/// <summary>
/// Tag equals filter expression.
/// </summary>
public class TagEqualsFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly string _value;
    
    public TagEqualsFilter(string fieldName, string value)
    {
        _fieldName = fieldName;
        _value = value;
    }
    
    public override string ToQueryString() => $"@{_fieldName}:{{{EscapeTagValue(_value)}}}";
    
    private static string EscapeTagValue(string value)
    {
        return value.Replace("-", "\\-").Replace(" ", "\\ ");
    }
}

/// <summary>
/// Tag not equals filter expression.
/// </summary>
public class TagNotEqualsFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly string _value;
    
    public TagNotEqualsFilter(string fieldName, string value)
    {
        _fieldName = fieldName;
        _value = value;
    }
    
    public override string ToQueryString() => $"-@{_fieldName}:{{{EscapeTagValue(_value)}}}";
    
    private static string EscapeTagValue(string value)
    {
        return value.Replace("-", "\\-").Replace(" ", "\\ ");
    }
}

/// <summary>
/// Tag IN filter expression.
/// </summary>
public class TagInFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly string[] _values;
    
    public TagInFilter(string fieldName, string[] values)
    {
        _fieldName = fieldName;
        _values = values;
    }
    
    public override string ToQueryString()
    {
        var escaped = _values.Select(v => v.Replace("-", "\\-").Replace(" ", "\\ "));
        return $"@{_fieldName}:{{{string.Join("|", escaped)}}}";
    }
}
