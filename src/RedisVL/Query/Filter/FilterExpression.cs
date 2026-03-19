namespace RedisVL.Query.Filter;

/// <summary>
/// Base class for filter expressions used in queries.
/// </summary>
public abstract class FilterExpression
{
    /// <summary>
    /// Converts the filter to a RediSearch query string.
    /// </summary>
    public abstract string ToQueryString();
    
    /// <summary>
    /// Combines two filters with AND logic.
    /// </summary>
    public static FilterExpression operator &(FilterExpression left, FilterExpression right)
        => new AndFilter(left, right);
    
    /// <summary>
    /// Combines two filters with OR logic.
    /// </summary>
    public static FilterExpression operator |(FilterExpression left, FilterExpression right)
        => new OrFilter(left, right);
    
    /// <summary>
    /// Negates a filter.
    /// </summary>
    public static FilterExpression operator ~(FilterExpression filter)
        => new NotFilter(filter);
    
    public override string ToString() => ToQueryString();
}

/// <summary>
/// AND combination of filters.
/// </summary>
public class AndFilter : FilterExpression
{
    private readonly FilterExpression _left;
    private readonly FilterExpression _right;
    
    public AndFilter(FilterExpression left, FilterExpression right)
    {
        _left = left;
        _right = right;
    }
    
    public override string ToQueryString() => $"({_left.ToQueryString()} {_right.ToQueryString()})";
}

/// <summary>
/// OR combination of filters.
/// </summary>
public class OrFilter : FilterExpression
{
    private readonly FilterExpression _left;
    private readonly FilterExpression _right;
    
    public OrFilter(FilterExpression left, FilterExpression right)
    {
        _left = left;
        _right = right;
    }
    
    public override string ToQueryString() => $"({_left.ToQueryString()} | {_right.ToQueryString()})";
}

/// <summary>
/// NOT filter (negation).
/// </summary>
public class NotFilter : FilterExpression
{
    private readonly FilterExpression _inner;
    
    public NotFilter(FilterExpression inner)
    {
        _inner = inner;
    }
    
    public override string ToQueryString() => $"-{_inner.ToQueryString()}";
}
