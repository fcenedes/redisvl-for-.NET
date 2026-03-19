namespace RedisVL.Extensions.Router;

/// <summary>
/// A route definition for semantic routing.
/// </summary>
public class Route
{
    /// <summary>
    /// The route name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference phrases that define this route.
    /// </summary>
    public IList<string> References { get; set; } = new List<string>();
    
    /// <summary>
    /// Optional metadata associated with the route.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Distance threshold for matching this route (lower = stricter).
    /// </summary>
    public double DistanceThreshold { get; set; } = 0.5;
}

/// <summary>
/// Result of a semantic routing operation.
/// </summary>
public class RouteMatch
{
    /// <summary>
    /// The matched route name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The semantic distance from the query.
    /// </summary>
    public double Distance { get; set; }
    
    /// <summary>
    /// The metadata from the matched route.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// The reference phrase that was closest to the query.
    /// </summary>
    public string? MatchedReference { get; set; }
}
