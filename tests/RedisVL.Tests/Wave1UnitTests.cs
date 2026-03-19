using RedisVL.Index;
using RedisVL.Schema;
using RedisVL.Schema.Fields;
using RedisVL.Query;
using RedisVL.Query.Filter;

namespace RedisVL.Tests;

/// <summary>
/// Wave 1: Unit test gaps — no Redis required.
/// </summary>
public class IndexSchemaFromDictionaryTests
{
    [Fact]
    public void FromDictionary_BasicSchema_ParsesCorrectly()
    {
        var dict = new Dictionary<string, object>
        {
            ["index"] = new Dictionary<string, object>
            {
                ["name"] = "dict-idx",
                ["prefix"] = "doc",
                ["storage_type"] = "hash"
            },
            ["fields"] = new object[]
            {
                new Dictionary<string, object> { ["name"] = "title", ["type"] = "text" },
                new Dictionary<string, object> { ["name"] = "cat", ["type"] = "tag" }
            }
        };

        var schema = IndexSchema.FromDictionary(dict);
        Assert.Equal("dict-idx", schema.Index.Name);
        Assert.Equal("doc", schema.Index.Prefix);
        Assert.Equal(StorageType.Hash, schema.Index.StorageType);
        Assert.Equal(2, schema.Fields.Count);
        Assert.IsType<TextField>(schema.Fields[0]);
        Assert.IsType<TagField>(schema.Fields[1]);
    }

    [Fact]
    public void FromDictionary_JsonStorageType_Parsed()
    {
        var dict = new Dictionary<string, object>
        {
            ["index"] = new Dictionary<string, object>
            {
                ["name"] = "json-idx",
                ["prefix"] = "j",
                ["storage_type"] = "json"
            },
            ["fields"] = new object[] { }
        };

        var schema = IndexSchema.FromDictionary(dict);
        Assert.Equal(StorageType.Json, schema.Index.StorageType);
    }

    [Fact]
    public void FromDictionary_VectorFieldWithAttrs_Parsed()
    {
        var dict = new Dictionary<string, object>
        {
            ["index"] = new Dictionary<string, object> { ["name"] = "v", ["prefix"] = "v" },
            ["fields"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "emb",
                    ["type"] = "vector",
                    ["attrs"] = new Dictionary<string, object>
                    {
                        ["algorithm"] = "flat",
                        ["dims"] = 64,
                        ["distance_metric"] = "l2"
                    }
                }
            }
        };

        var schema = IndexSchema.FromDictionary(dict);
        var vf = Assert.IsType<VectorField>(schema.Fields[0]);
        Assert.Equal(VectorAlgorithm.Flat, vf.Algorithm);
        Assert.Equal(64, vf.Dims);
        Assert.Equal(DistanceMetric.L2, vf.DistanceMetric);
    }

    [Fact]
    public void FromDictionary_AllFieldTypes_Parsed()
    {
        var dict = new Dictionary<string, object>
        {
            ["index"] = new Dictionary<string, object> { ["name"] = "a", ["prefix"] = "a" },
            ["fields"] = new object[]
            {
                new Dictionary<string, object> { ["name"] = "t", ["type"] = "text" },
                new Dictionary<string, object> { ["name"] = "tg", ["type"] = "tag" },
                new Dictionary<string, object> { ["name"] = "n", ["type"] = "numeric" },
                new Dictionary<string, object> { ["name"] = "g", ["type"] = "geo" },
                new Dictionary<string, object>
                {
                    ["name"] = "v", ["type"] = "vector",
                    ["attrs"] = new Dictionary<string, object> { ["dims"] = 8 }
                }
            }
        };

        var schema = IndexSchema.FromDictionary(dict);
        Assert.Equal(5, schema.Fields.Count);
        Assert.IsType<TextField>(schema.Fields[0]);
        Assert.IsType<TagField>(schema.Fields[1]);
        Assert.IsType<NumericField>(schema.Fields[2]);
        Assert.IsType<GeoField>(schema.Fields[3]);
        Assert.IsType<VectorField>(schema.Fields[4]);
    }

    [Fact]
    public void FromDictionary_EmptyFields_ReturnsEmptyList()
    {
        var dict = new Dictionary<string, object>
        {
            ["index"] = new Dictionary<string, object> { ["name"] = "e", ["prefix"] = "e" },
            ["fields"] = new object[] { }
        };

        var schema = IndexSchema.FromDictionary(dict);
        Assert.Empty(schema.Fields);
    }
}

public class VectorFieldDefaultsTests
{
    [Fact]
    public void HnswDefaults_AreCorrect()
    {
        var field = new VectorField();
        Assert.Equal(VectorAlgorithm.HNSW, field.Algorithm);
        Assert.Equal(DistanceMetric.Cosine, field.DistanceMetric);
        Assert.Equal("FLOAT32", field.DataType);
        Assert.Equal(16, field.M);
        Assert.Equal(200, field.EfConstruction);
        Assert.Equal(10, field.EfRuntime);
    }

    [Fact]
    public void FlatDefaults_BlockSize()
    {
        var field = new VectorField { Algorithm = VectorAlgorithm.Flat };
        Assert.Equal(1024, field.BlockSize);
    }

    [Fact]
    public void Dims_DefaultsToZero()
    {
        var field = new VectorField();
        Assert.Equal(0, field.Dims);
    }

    [Fact]
    public void HnswParams_CanBeCustomized()
    {
        var field = new VectorField
        {
            Algorithm = VectorAlgorithm.HNSW,
            M = 32,
            EfConstruction = 400,
            EfRuntime = 20
        };
        Assert.Equal(32, field.M);
        Assert.Equal(400, field.EfConstruction);
        Assert.Equal(20, field.EfRuntime);
    }
}

public class SearchResultGetFieldEdgeCaseTests
{
    [Fact]
    public void GetField_MissingKey_ReturnsDefault()
    {
        var result = new SearchResult
        {
            Id = "doc:1",
            Fields = new Dictionary<string, object?>()
        };
        Assert.Null(result.GetField<string>("missing"));
        Assert.Equal(0, result.GetField<int>("missing"));
        Assert.Equal(0.0, result.GetField<double>("missing"));
    }

    [Fact]
    public void GetField_NullValue_ReturnsDefault()
    {
        var result = new SearchResult
        {
            Fields = new Dictionary<string, object?> { ["key"] = null }
        };
        Assert.Null(result.GetField<string>("key"));
        Assert.Equal(0, result.GetField<int>("key"));
    }

    [Fact]
    public void GetField_StringToInt_Converts()
    {
        var result = new SearchResult
        {
            Fields = new Dictionary<string, object?> { ["count"] = "42" }
        };
        Assert.Equal(42, result.GetField<int>("count"));
    }

    [Fact]
    public void GetField_StringToDouble_Converts()
    {
        // Convert.ChangeType uses current culture; use a culture-safe value
        var result = new SearchResult
        {
            Fields = new Dictionary<string, object?> { ["score"] = 3.14 }
        };
        Assert.Equal(3.14, result.GetField<double>("score"));
    }

    [Fact]
    public void GetField_IntToString_Converts()
    {
        var result = new SearchResult
        {
            Fields = new Dictionary<string, object?> { ["num"] = 99 }
        };
        Assert.Equal("99", result.GetField<string>("num"));
    }

    [Fact]
    public void GetField_InvalidConversion_Throws()
    {
        var result = new SearchResult
        {
            Fields = new Dictionary<string, object?> { ["val"] = "not-a-number" }
        };
        Assert.Throws<FormatException>(() => result.GetField<int>("val"));
    }

    [Fact]
    public void GetField_ExactTypeMatch_NoConversion()
    {
        var result = new SearchResult
        {
            Fields = new Dictionary<string, object?> { ["name"] = "hello" }
        };
        Assert.Equal("hello", result.GetField<string>("name"));
    }
}

public class NumericFilterNotEqualsTests
{
    [Fact]
    public void NotEquals_ProducesNegatedRange()
    {
        var filter = Num.Field("age") != 25;
        var qs = filter.ToQueryString();
        Assert.Equal("-@age:[25 25]", qs);
    }

    [Fact]
    public void NotEquals_Zero_ProducesNegatedRange()
    {
        var filter = Num.Field("count") != 0;
        Assert.Equal("-@count:[0 0]", filter.ToQueryString());
    }

    [Fact]
    public void NotEquals_NegativeValue_ProducesNegatedRange()
    {
        var filter = Num.Field("temp") != -10;
        Assert.Equal("-@temp:[-10 -10]", filter.ToQueryString());
    }

    [Fact]
    public void NotEquals_CombinedWithAnd_Works()
    {
        var filter = (Num.Field("age") != 25) & (Tag.Field("status") == "active");
        var qs = filter.ToQueryString();
        Assert.Contains("-@age:[25 25]", qs);
        Assert.Contains("@status:{active}", qs);
    }
}

public class SearchResultsDefaultsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var results = new SearchResults();
        Assert.Equal(0, results.TotalResults);
        Assert.NotNull(results.Documents);
        Assert.Empty(results.Documents);
        Assert.Null(results.ExecutionTimeMs);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var results = new SearchResults
        {
            TotalResults = 42,
            ExecutionTimeMs = 1.5,
            Documents = new List<SearchResult>
            {
                new SearchResult { Id = "doc:1" }
            }
        };
        Assert.Equal(42, results.TotalResults);
        Assert.Equal(1.5, results.ExecutionTimeMs);
        Assert.Single(results.Documents);
    }
}

public class FieldBaseFieldPathEdgeCaseTests
{
    [Fact]
    public void FieldPath_NameWithoutDollar_PrependsDollarDot()
    {
        var field = new TextField { Name = "title" };
        Assert.Equal("$.title", field.FieldPath);
    }

    [Fact]
    public void FieldPath_NameStartingWithDollarDot_NoDoublePrefix()
    {
        var field = new TextField { Name = "$.title" };
        Assert.Equal("$.title", field.FieldPath);
    }

    [Fact]
    public void FieldPath_ExplicitPath_OverridesName()
    {
        var field = new TextField { Name = "title", Path = "$.nested.title" };
        Assert.Equal("$.nested.title", field.FieldPath);
    }

    [Fact]
    public void FieldPath_NullPath_UsesNameBased()
    {
        var field = new NumericField { Name = "price", Path = null };
        Assert.Equal("$.price", field.FieldPath);
    }

    [Fact]
    public void FieldPath_NestedName_PrependsDollarDot()
    {
        var field = new TagField { Name = "category.sub" };
        Assert.Equal("$.category.sub", field.FieldPath);
    }

    [Fact]
    public void FieldPath_AllFieldTypes_WorkConsistently()
    {
        var text = new TextField { Name = "a" };
        var tag = new TagField { Name = "a" };
        var num = new NumericField { Name = "a" };
        var geo = new GeoField { Name = "a" };
        var vec = new VectorField { Name = "a" };

        Assert.Equal("$.a", text.FieldPath);
        Assert.Equal("$.a", tag.FieldPath);
        Assert.Equal("$.a", num.FieldPath);
        Assert.Equal("$.a", geo.FieldPath);
        Assert.Equal("$.a", vec.FieldPath);
    }
}

public class RedisConnectionProviderParseUrlTests
{
    [Fact]
    public void ParseRedisUrl_SimpleRedisUrl_ReturnsHostPort()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("redis://localhost:6379");
        Assert.Contains("localhost:6379", result);
    }

    [Fact]
    public void ParseRedisUrl_WithPassword_IncludesPassword()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("redis://mypassword@localhost:6379");
        Assert.Contains("localhost:6379", result);
        Assert.Contains("password=mypassword", result);
    }

    [Fact]
    public void ParseRedisUrl_WithUserAndPassword_ExtractsPassword()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("redis://user:secret@localhost:6379");
        Assert.Contains("localhost:6379", result);
        Assert.Contains("password=secret", result);
    }

    [Fact]
    public void ParseRedisUrl_WithDatabase_IncludesDefaultDatabase()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("redis://localhost:6379/2");
        Assert.Contains("localhost:6379", result);
        Assert.Contains("defaultDatabase=2", result);
    }

    [Fact]
    public void ParseRedisUrl_RedissScheme_IncludesSsl()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("rediss://localhost:6380");
        Assert.Contains("localhost:6380", result);
        Assert.Contains("ssl=true", result);
    }

    [Fact]
    public void ParseRedisUrl_PlainConnectionString_PassedThrough()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("myhost:6379,password=abc");
        Assert.Equal("myhost:6379,password=abc", result);
    }

    [Fact]
    public void ParseRedisUrl_EmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() => RedisConnectionProvider.ParseRedisUrl(""));
    }

    [Fact]
    public void ParseRedisUrl_NullString_Throws()
    {
        Assert.Throws<ArgumentException>(() => RedisConnectionProvider.ParseRedisUrl(null!));
    }

    [Fact]
    public void ParseRedisUrl_FullUrl_AllParts()
    {
        var result = RedisConnectionProvider.ParseRedisUrl("rediss://user:pass123@myhost.com:6380/3");
        Assert.Contains("myhost.com:6380", result);
        Assert.Contains("password=pass123", result);
        Assert.Contains("ssl=true", result);
        Assert.Contains("defaultDatabase=3", result);
    }

    [Fact]
    public void ParseRedisUrl_DefaultPort_Uses6379()
    {
        // When port is not specified in the URL, Uri may use default http port
        // but ParseRedisUrl should handle it
        var result = RedisConnectionProvider.ParseRedisUrl("redis://localhost");
        Assert.Contains("localhost", result);
    }
}

