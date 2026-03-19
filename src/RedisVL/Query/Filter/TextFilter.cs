namespace RedisVL.Query.Filter;

/// <summary>
/// Builder for Text field filters.
/// </summary>
public class Text
{
    private readonly string _fieldName;
    
    private Text(string fieldName)
    {
        _fieldName = fieldName;
    }
    
    /// <summary>
    /// Creates a text filter builder for the specified field.
    /// </summary>
    public static Text Field(string fieldName) => new(fieldName);
    
    /// <summary>
    /// Matches the text value.
    /// </summary>
    public TextMatchFilter Match(string value) => new(_fieldName, value);
    
    /// <summary>
    /// Wildcard match.
    /// </summary>
    public TextMatchFilter Like(string pattern) => new(_fieldName, pattern, isWildcard: true);
}

/// <summary>
/// Text match filter expression.
/// </summary>
public class TextMatchFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly string _value;
    private readonly bool _isWildcard;
    
    public TextMatchFilter(string fieldName, string value, bool isWildcard = false)
    {
        _fieldName = fieldName;
        _value = value;
        _isWildcard = isWildcard;
    }
    
    public override string ToQueryString()
    {
        if (_isWildcard)
            return $"@{_fieldName}:{_value}";
        return $"@{_fieldName}:({_value})";
    }
}
