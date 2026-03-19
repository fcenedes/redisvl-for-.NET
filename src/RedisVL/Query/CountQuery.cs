namespace RedisVL.Query;

/// <summary>
/// Query to count matching documents.
/// </summary>
public class CountQuery : BaseQuery
{
    public CountQuery() { }
    
    public CountQuery(Filter.FilterExpression filter)
    {
        FilterExpression = filter;
    }
    
    public override string GetQueryString() => GetFilterString();
}
