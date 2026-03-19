namespace RedisVL.Query;

/// <summary>
/// Filter-only query without vector search.
/// </summary>
public class FilterQuery : BaseQuery
{
    /// <summary>
    /// Sort by field name.
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Sort in ascending order.
    /// </summary>
    public bool SortAscending { get; set; } = true;
    
    public FilterQuery() { }
    
    public FilterQuery(Filter.FilterExpression filter)
    {
        FilterExpression = filter;
    }
    
    public override string GetQueryString() => GetFilterString();
}
