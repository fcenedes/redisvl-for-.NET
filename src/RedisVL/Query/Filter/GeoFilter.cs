using System.Globalization;

namespace RedisVL.Query.Filter;

/// <summary>
/// Geographic distance unit.
/// </summary>
public enum GeoUnit
{
    Meters,
    Kilometers,
    Miles,
    Feet
}

/// <summary>
/// Builder for Geo field filters.
/// </summary>
public class Geo
{
    private readonly string _fieldName;
    
    private Geo(string fieldName)
    {
        _fieldName = fieldName;
    }
    
    /// <summary>
    /// Creates a geo filter builder for the specified field.
    /// </summary>
    public static Geo Field(string fieldName) => new(fieldName);
    
    /// <summary>
    /// Creates a radius filter centered at the specified coordinates.
    /// </summary>
    public GeoRadiusFilter WithinRadius(double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Kilometers)
        => new(_fieldName, longitude, latitude, radius, unit);
}

/// <summary>
/// Geo radius filter expression.
/// </summary>
public class GeoRadiusFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly double _longitude;
    private readonly double _latitude;
    private readonly double _radius;
    private readonly GeoUnit _unit;
    
    public GeoRadiusFilter(string fieldName, double longitude, double latitude, double radius, GeoUnit unit)
    {
        _fieldName = fieldName;
        _longitude = longitude;
        _latitude = latitude;
        _radius = radius;
        _unit = unit;
    }
    
    public override string ToQueryString()
    {
        var unitStr = _unit switch
        {
            GeoUnit.Meters => "m",
            GeoUnit.Kilometers => "km",
            GeoUnit.Miles => "mi",
            GeoUnit.Feet => "ft",
            _ => "km"
        };
        return $"@{_fieldName}:[{_longitude.ToString(CultureInfo.InvariantCulture)} {_latitude.ToString(CultureInfo.InvariantCulture)} {_radius.ToString(CultureInfo.InvariantCulture)} {unitStr}]";
    }
}
