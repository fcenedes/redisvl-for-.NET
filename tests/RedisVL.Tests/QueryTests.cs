using RedisVL.Query;
using RedisVL.Query.Filter;

namespace RedisVL.Tests;

public class ExtendedQueryTests
{
    [Fact]
    public void VectorQuery_WithFilterExpression_IncludesFilterInQueryString()
    {
        var query = new VectorQuery(new float[] { 1.0f, 2.0f, 3.0f }, "embedding", 10)
        {
            FilterExpression = Tag.Field("status") == "active"
        };
        var qs = query.GetQueryString();

        Assert.Contains("@status:{active}", qs);
        Assert.Contains("KNN 10 @embedding", qs);
    }

    [Fact]
    public void VectorQuery_WithEfRuntime_IncludesEfRuntimeParam()
    {
        var query = new VectorQuery(new float[] { 0.5f }, "vec", 5)
        {
            EfRuntime = 200
        };
        var qs = query.GetQueryString();

        Assert.Contains("EF_RUNTIME 200", qs);
        Assert.Contains("KNN 5 @vec", qs);
    }

    [Fact]
    public void VectorQuery_GetVectorBytes_CorrectLengthForVariousSizes()
    {
        var small = new VectorQuery(new float[] { 1.0f }, "e");
        Assert.Equal(sizeof(float), small.GetVectorBytes().Length);

        var medium = new VectorQuery(new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, "e");
        Assert.Equal(5 * sizeof(float), medium.GetVectorBytes().Length);

        var large = new float[1536];
        var largeQ = new VectorQuery(large, "e");
        Assert.Equal(1536 * sizeof(float), largeQ.GetVectorBytes().Length);
    }

    [Fact]
    public void VectorQuery_CustomScoreFieldName_UsedInQueryString()
    {
        var query = new VectorQuery(new float[] { 1.0f }, "embedding", 5)
        {
            ScoreFieldName = "my_score"
        };
        var qs = query.GetQueryString();

        Assert.Contains("AS my_score", qs);
        Assert.DoesNotContain("vector_distance", qs);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void RangeQuery_WithVariousDistanceThresholds_CorrectFormat(double threshold)
    {
        var query = new RangeQuery(new float[] { 1.0f }, "embedding", threshold);
        var qs = query.GetQueryString();

        Assert.Contains("VECTOR_RANGE", qs);
        Assert.Contains(threshold.ToString(System.Globalization.CultureInfo.InvariantCulture), qs);
        Assert.Contains("@embedding", qs);
    }

    [Fact]
    public void RangeQuery_WithFilter_IncludesFilterAndRange()
    {
        var query = new RangeQuery(new float[] { 1.0f }, "embedding", 0.3)
        {
            FilterExpression = Num.Field("price") >= 100
        };
        var qs = query.GetQueryString();

        Assert.Contains("@price:[100 +inf]", qs);
        Assert.Contains("VECTOR_RANGE", qs);
    }

    [Fact]
    public void RangeQuery_GetVectorBytes_CorrectLength()
    {
        var vector = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var query = new RangeQuery(vector, "embedding", 0.5);
        Assert.Equal(4 * sizeof(float), query.GetVectorBytes().Length);
    }

    [Fact]
    public void HybridQuery_WithLinearCombination_CorrectFormat()
    {
        var query = new HybridQuery(new float[] { 1.0f }, "embedding", "hello", "content")
        {
            CombinationMethod = HybridCombinationMethod.Linear
        };
        var qs = query.GetQueryString();

        Assert.Contains("LINEAR", qs);
        Assert.Contains("HYBRID", qs);
    }

    [Fact]
    public void HybridQuery_WithRRFCombination_CorrectFormat()
    {
        var query = new HybridQuery(new float[] { 1.0f }, "embedding", "hello", "content")
        {
            CombinationMethod = HybridCombinationMethod.RRF
        };
        var qs = query.GetQueryString();

        Assert.Contains("RRF", qs);
    }

    [Fact]
    public void HybridQuery_WithWeights_HasCorrectDefaults()
    {
        var query = new HybridQuery(new float[] { 1.0f }, "embedding", "text", "content");
        Assert.Equal(0.5, query.VectorWeight);
        Assert.Equal(0.5, query.TextWeight);

        query.VectorWeight = 0.7;
        query.TextWeight = 0.3;
        Assert.Equal(0.7, query.VectorWeight);
        Assert.Equal(0.3, query.TextWeight);
    }

    [Fact]
    public void HybridQuery_WithFilter_IncludesFilter()
    {
        var query = new HybridQuery(new float[] { 1.0f }, "embedding", "search", "content")
        {
            FilterExpression = Tag.Field("category") == "tech"
        };
        var qs = query.GetQueryString();

        Assert.Contains("@category:{tech}", qs);
        Assert.Contains("HYBRID", qs);
    }

    [Fact]
    public void TextQuery_WithDifferentFields_CorrectFormat()
    {
        var q1 = new TextQuery("redis", "title");
        Assert.Equal("@title:(redis)", q1.GetQueryString());

        var q2 = new TextQuery("redis", "description");
        Assert.Equal("@description:(redis)", q2.GetQueryString());
    }

    [Fact]
    public void TextQuery_ExactMatchSyntax_WithQuotes()
    {
        var query = new TextQuery("\"exact phrase\"", "content");
        var qs = query.GetQueryString();

        Assert.Equal("@content:(\"exact phrase\")", qs);
    }

    [Fact]
    public void CountQuery_WithComplexFilter_CorrectFormat()
    {
        var filter = (Tag.Field("status") == "active") & (Num.Field("price") >= 50);
        var query = new CountQuery(filter);
        var qs = query.GetQueryString();

        Assert.Contains("@status:{active}", qs);
        Assert.Contains("@price:[50 +inf]", qs);
    }

    [Fact]
    public void BaseQuery_Dialect_DefaultsToTwo()
    {
        var query = new VectorQuery(new float[] { 1.0f }, "embedding");
        Assert.Equal(2, query.Dialect);

        query.Dialect = 3;
        Assert.Equal(3, query.Dialect);
    }

    [Fact]
    public void BaseQuery_OffsetAndNumResults_Pagination()
    {
        var query = new VectorQuery(new float[] { 1.0f }, "embedding")
        {
            Offset = 20,
            NumResults = 10
        };

        Assert.Equal(20, query.Offset);
        Assert.Equal(10, query.NumResults);
    }

    [Fact]
    public void BaseQuery_ReturnFields_CanBeSet()
    {
        var query = new FilterQuery
        {
            ReturnFields = new[] { "title", "price", "category" }
        };

        Assert.Equal(3, query.ReturnFields!.Length);
        Assert.Contains("title", query.ReturnFields);
    }

    [Fact]
    public void VectorQuery_DefaultValues_AreCorrect()
    {
        var query = new VectorQuery();
        Assert.Equal("embedding", query.VectorFieldName);
        Assert.True(query.ReturnScore);
        Assert.Equal("vector_distance", query.ScoreFieldName);
        Assert.Null(query.EfRuntime);
        Assert.Equal(10, query.NumResults);
    }
}
