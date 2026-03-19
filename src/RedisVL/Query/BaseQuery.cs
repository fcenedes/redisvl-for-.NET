using RedisVL.Query.Filter;

namespace RedisVL.Query;

/// <summary>
/// Base class for all query types.
/// </summary>
public abstract class BaseQuery
{
    /// <summary>
    /// Filter expression to apply.
    /// </summary>
    public FilterExpression? FilterExpression { get; set; }
    
    /// <summary>
    /// Fields to return in results.
    /// </summary>
    public string[]? ReturnFields { get; set; }
    
    /// <summary>
    /// Number of results to return.
    /// </summary>
    public int NumResults { get; set; } = 10;
    
    /// <summary>
    /// Offset for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;
    
    /// <summary>
    /// Dialect version for the query.
    /// </summary>
    public int Dialect { get; set; } = 2;
    
    /// <summary>
    /// Gets the query string for RediSearch.
    /// </summary>
    public abstract string GetQueryString();
    
    /// <summary>
    /// Gets the base filter string or "*" for all.
    /// </summary>
    protected string GetFilterString() => FilterExpression?.ToQueryString() ?? "*";
}
