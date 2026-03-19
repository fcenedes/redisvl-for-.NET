using System.Globalization;

namespace RedisVL.Query.Filter;

/// <summary>
/// Builder for Timestamp field filters. Converts DateTime/DateTimeOffset/Unix timestamps
/// to numeric range queries on Redis numeric fields.
/// </summary>
public class Timestamp
{
    private readonly string _fieldName;

    private Timestamp(string fieldName)
    {
        _fieldName = fieldName;
    }

    /// <summary>
    /// Creates a timestamp filter builder for the specified field.
    /// </summary>
    public static Timestamp Field(string fieldName) => new(fieldName);

    /// <summary>
    /// Equals filter — for DateTime with time component, matches the exact timestamp.
    /// For date-only values (midnight), matches the entire day.
    /// </summary>
    public static FilterExpression operator ==(Timestamp ts, DateTime value)
    {
        if (value.TimeOfDay == TimeSpan.Zero)
        {
            // Date-only: match entire day
            var startOfDay = ToUnixSeconds(value);
            var endOfDay = ToUnixSeconds(value.Date.AddDays(1).AddTicks(-1));
            return new TimestampRangeFilter(ts._fieldName, startOfDay, endOfDay);
        }
        var unix = ToUnixSeconds(value);
        return new NumericEqualsFilter(ts._fieldName, unix);
    }

    public static FilterExpression operator !=(Timestamp ts, DateTime value)
    {
        var unix = ToUnixSeconds(value);
        return new NumericNotEqualsFilter(ts._fieldName, unix);
    }

    public static FilterExpression operator >(Timestamp ts, DateTime value)
        => new TimestampComparisonFilter(ts._fieldName, ToUnixSeconds(value), ">");

    public static FilterExpression operator <(Timestamp ts, DateTime value)
        => new TimestampComparisonFilter(ts._fieldName, ToUnixSeconds(value), "<");

    public static FilterExpression operator >=(Timestamp ts, DateTime value)
        => new TimestampComparisonFilter(ts._fieldName, ToUnixSeconds(value), ">=");

    public static FilterExpression operator <=(Timestamp ts, DateTime value)
        => new TimestampComparisonFilter(ts._fieldName, ToUnixSeconds(value), "<=");

    // DateTimeOffset overloads
    public static FilterExpression operator ==(Timestamp ts, DateTimeOffset value)
        => ts == value.UtcDateTime;

    public static FilterExpression operator !=(Timestamp ts, DateTimeOffset value)
        => ts != value.UtcDateTime;

    public static FilterExpression operator >(Timestamp ts, DateTimeOffset value)
        => ts > value.UtcDateTime;

    public static FilterExpression operator <(Timestamp ts, DateTimeOffset value)
        => ts < value.UtcDateTime;

    public static FilterExpression operator >=(Timestamp ts, DateTimeOffset value)
        => ts >= value.UtcDateTime;

    public static FilterExpression operator <=(Timestamp ts, DateTimeOffset value)
        => ts <= value.UtcDateTime;

    // Unix timestamp (long) overloads
    public static FilterExpression operator ==(Timestamp ts, long unixSeconds)
        => new NumericEqualsFilter(ts._fieldName, unixSeconds);

    public static FilterExpression operator !=(Timestamp ts, long unixSeconds)
        => new NumericNotEqualsFilter(ts._fieldName, unixSeconds);

    public static FilterExpression operator >(Timestamp ts, long unixSeconds)
        => new TimestampComparisonFilter(ts._fieldName, unixSeconds, ">");

    public static FilterExpression operator <(Timestamp ts, long unixSeconds)
        => new TimestampComparisonFilter(ts._fieldName, unixSeconds, "<");

    public static FilterExpression operator >=(Timestamp ts, long unixSeconds)
        => new TimestampComparisonFilter(ts._fieldName, unixSeconds, ">=");

    public static FilterExpression operator <=(Timestamp ts, long unixSeconds)
        => new TimestampComparisonFilter(ts._fieldName, unixSeconds, "<=");

    /// <summary>
    /// Range filter — matches timestamps between start and end (inclusive).
    /// </summary>
    public FilterExpression Between(DateTime start, DateTime end)
        => new TimestampRangeFilter(_fieldName, ToUnixSeconds(start), ToUnixSeconds(end));

    /// <summary>
    /// Range filter — matches timestamps between start and end (inclusive).
    /// </summary>
    public FilterExpression Between(DateTimeOffset start, DateTimeOffset end)
        => Between(start.UtcDateTime, end.UtcDateTime);

    /// <summary>
    /// Range filter — matches timestamps between start and end (inclusive).
    /// </summary>
    public FilterExpression Between(long startUnix, long endUnix)
        => new TimestampRangeFilter(_fieldName, startUnix, endUnix);

    internal static long ToUnixSeconds(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Unspecified)
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return new DateTimeOffset(dt).ToUnixTimeSeconds();
    }

    public override bool Equals(object? obj) => obj is Timestamp other && _fieldName == other._fieldName;
    public override int GetHashCode() => _fieldName.GetHashCode();
}

/// <summary>
/// Numeric equality filter (exact match).
/// </summary>
internal class NumericEqualsFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly long _value;

    public NumericEqualsFilter(string fieldName, long value)
    {
        _fieldName = fieldName;
        _value = value;
    }

    public override string ToQueryString() => $"@{_fieldName}:[{_value} {_value}]";
}

/// <summary>
/// Numeric inequality filter (negated exact match).
/// </summary>
internal class NumericNotEqualsFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly long _value;

    public NumericNotEqualsFilter(string fieldName, long value)
    {
        _fieldName = fieldName;
        _value = value;
    }

    public override string ToQueryString() => $"-@{_fieldName}:[{_value} {_value}]";
}

/// <summary>
/// Timestamp comparison filter (>, <, >=, <=).
/// </summary>
internal class TimestampComparisonFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly long _value;
    private readonly string _op;

    public TimestampComparisonFilter(string fieldName, long value, string op)
    {
        _fieldName = fieldName;
        _value = value;
        _op = op;
    }

    public override string ToQueryString() => _op switch
    {
        ">" => $"@{_fieldName}:[({_value} +inf]",
        ">=" => $"@{_fieldName}:[{_value} +inf]",
        "<" => $"@{_fieldName}:[-inf ({_value}]",
        "<=" => $"@{_fieldName}:[-inf {_value}]",
        _ => throw new InvalidOperationException($"Unknown operator: {_op}")
    };
}

/// <summary>
/// Timestamp range filter (between, inclusive).
/// </summary>
internal class TimestampRangeFilter : FilterExpression
{
    private readonly string _fieldName;
    private readonly long _start;
    private readonly long _end;

    public TimestampRangeFilter(string fieldName, long start, long end)
    {
        _fieldName = fieldName;
        _start = start;
        _end = end;
    }

    public override string ToQueryString() => $"@{_fieldName}:[{_start} {_end}]";
}

