using RedisVL.Query.Filter;

namespace RedisVL.Tests;

public class ExtendedFilterTests
{
    [Fact]
    public void DeeplyNestedFilter_CombinesCorrectly()
    {
        var a = Tag.Field("a") == "1";
        var b = Tag.Field("b") == "2";
        var c = Tag.Field("c") == "3";
        var d = Tag.Field("d") == "4";
        var e = Num.Field("e") >= 5;

        var combined = ((a & b) | (c & d)) & e;
        var qs = combined.ToQueryString();

        Assert.Contains("@a:{1}", qs);
        Assert.Contains("@b:{2}", qs);
        Assert.Contains("@c:{3}", qs);
        Assert.Contains("@d:{4}", qs);
        Assert.Contains("@e:[5 +inf]", qs);
        Assert.StartsWith("(", qs);
    }

    [Fact]
    public void TagFilter_WithMultipleSpecialCharacters_EscapesCorrectly()
    {
        var filter = Tag.Field("name") == "hello-world test";
        var qs = filter.ToQueryString();

        Assert.Contains("hello\\-world\\ test", qs);
    }

    [Fact]
    public void TagFilter_In_WithManyValues_CorrectSyntax()
    {
        var filter = Tag.Field("color").In("red", "blue", "green", "yellow", "orange", "purple", "pink");
        var qs = filter.ToQueryString();

        Assert.Equal("@color:{red|blue|green|yellow|orange|purple|pink}", qs);
    }

    [Fact]
    public void NumericFilter_IntMinAndMax_CorrectSyntax()
    {
        var filterMin = Num.Field("val") >= int.MinValue;
        Assert.Contains(int.MinValue.ToString(), filterMin.ToQueryString());

        var filterMax = Num.Field("val") <= int.MaxValue;
        Assert.Contains(int.MaxValue.ToString(), filterMax.ToQueryString());
    }

    [Fact]
    public void NumericFilter_DecimalValues_CorrectSyntax()
    {
        var filter = Num.Field("price").Between(9.99, 99.99);
        var qs = filter.ToQueryString();

        Assert.Equal("@price:[9.99 99.99]", qs);
    }

    [Fact]
    public void NumericFilter_GreaterThan_ExclusiveMin()
    {
        var filter = Num.Field("score") > 50;
        var qs = filter.ToQueryString();

        Assert.Equal("@score:[(50 +inf]", qs);
    }

    [Fact]
    public void NumericFilter_LessThanOrEqual_CorrectSyntax()
    {
        var filter = Num.Field("score") <= 100;
        var qs = filter.ToQueryString();

        Assert.Equal("@score:[-inf 100]", qs);
    }

    [Fact]
    public void GeoFilter_Kilometers_CorrectUnit()
    {
        var filter = Geo.Field("loc").WithinRadius(-73.9, 40.7, 10, GeoUnit.Kilometers);
        Assert.Contains("10 km", filter.ToQueryString());
    }

    [Fact]
    public void GeoFilter_Miles_CorrectUnit()
    {
        var filter = Geo.Field("loc").WithinRadius(-73.9, 40.7, 10, GeoUnit.Miles);
        Assert.Contains("10 mi", filter.ToQueryString());
    }

    [Fact]
    public void GeoFilter_Meters_CorrectUnit()
    {
        var filter = Geo.Field("loc").WithinRadius(0, 0, 500, GeoUnit.Meters);
        Assert.Contains("500 m", filter.ToQueryString());
    }

    [Fact]
    public void GeoFilter_Feet_CorrectUnit()
    {
        var filter = Geo.Field("loc").WithinRadius(0, 0, 1000, GeoUnit.Feet);
        Assert.Contains("1000 ft", filter.ToQueryString());
    }

    [Fact]
    public void GeoFilter_BoundaryValues_CorrectFormat()
    {
        var filter = Geo.Field("loc").WithinRadius(-180, -90, 0.001, GeoUnit.Kilometers);
        var qs = filter.ToQueryString();
        Assert.Contains("-180", qs);
        Assert.Contains("-90", qs);
        Assert.Contains("0.001", qs);
    }

    [Fact]
    public void TextFilter_WildcardPatterns_CorrectSyntax()
    {
        var prefix = Text.Field("name").Like("red*");
        Assert.Equal("@name:red*", prefix.ToQueryString());

        var suffix = Text.Field("name").Like("*base");
        Assert.Equal("@name:*base", suffix.ToQueryString());

        var pattern = Text.Field("name").Like("re?is");
        Assert.Equal("@name:re?is", pattern.ToQueryString());
    }

    [Fact]
    public void TextFilter_PhraseMatching_CorrectSyntax()
    {
        var filter = Text.Field("desc").Match("redis vector database");
        Assert.Equal("@desc:(redis vector database)", filter.ToQueryString());
    }

    [Fact]
    public void Filter_ToString_ReturnsSameAsToQueryString()
    {
        var filter = Tag.Field("status") == "active";
        Assert.Equal(filter.ToQueryString(), filter.ToString());

        var combined = filter & (Num.Field("score") >= 10);
        Assert.Equal(combined.ToQueryString(), combined.ToString());
    }

    [Fact]
    public void TagFilter_In_WithSpecialChars_EscapesCorrectly()
    {
        var filter = Tag.Field("tag").In("a-b", "c d");
        var qs = filter.ToQueryString();
        Assert.Contains("a\\-b", qs);
        Assert.Contains("c\\ d", qs);
    }
}

