using System.Globalization;

namespace RedisVL.Query.Filter;

/// <summary>
/// Builder for Numeric field filters.
/// </summary>
public class Num
{
    private readonly string _fieldName;
    
    private Num(string fieldName)
    {
        _fieldName = fieldName;
    }
    
    /// <summary>
    /// Creates a numeric filter builder for the specified field.
    /// </summary>
    public static Num Field(string fieldName) => new(fieldName);
    
    public static FilterExpression operator ==(Num num, double value)
        => new NumericRangeFilter(num._fieldName, value, value);
    
    public static FilterExpression operator !=(Num num, double value)
        => new NotFilter(new NumericRangeFilter(num._fieldName, value, value));
    
    public static NumericRangeFilter operator >(Num num, double value)
        => new(num._fieldName, value, double.PositiveInfinity, exclusiveMin: true);
    
    public static NumericRangeFilter operator >=(Num num, double value)
        => new(num._fieldName, value, double.PositiveInfinity);
    
    public static NumericRangeFilter operator <(Num num, double value)
        => new(num._fieldName, double.NegativeInfinity, value, exclusiveMax: true);
    
    public static NumericRangeFilter operator <=(Num num, double value)
        => new(num._fieldName, double.NegativeInfinity, value);
    
    /// <summary>
    /// Creates a between filter (inclusive).
    /// </summary>
    public NumericRangeFilter Between(double min, double max) => new(_fieldName, min, max);
    
    public override bool Equals(object? obj) => obj is Num other && _fieldName == other._fieldName;
    public override int GetHashCode() => _fieldName.GetHashCode();
}

/// <summary>
/// Numeric range filter expression.
/// </summary>
public class NumericRangeFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly double _min;
    private readonly double _max;
    private readonly bool _exclusiveMin;
    private readonly bool _exclusiveMax;
    
    public NumericRangeFilter(string fieldName, double min, double max, 
        bool exclusiveMin = false, bool exclusiveMax = false)
    {
        _fieldName = fieldName;
        _min = min;
        _max = max;
        _exclusiveMin = exclusiveMin;
        _exclusiveMax = exclusiveMax;
    }
    
    /// <summary>
    /// Combines with another range filter using AND.
    /// </summary>
    public static FilterExpression operator &(NumericRangeFilter left, NumericRangeFilter right)
        => new AndFilter(left, right);
    
    public override string ToQueryString()
    {
        var minStr = double.IsNegativeInfinity(_min) ? "-inf" : 
            (_exclusiveMin ? $"({_min.ToString(CultureInfo.InvariantCulture)}" : _min.ToString(CultureInfo.InvariantCulture));
        var maxStr = double.IsPositiveInfinity(_max) ? "+inf" : 
            (_exclusiveMax ? $"({_max.ToString(CultureInfo.InvariantCulture)}" : _max.ToString(CultureInfo.InvariantCulture));
        
        return $"@{_fieldName}:[{minStr} {maxStr}]";
    }
}
