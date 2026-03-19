using RedisVL.Index;
using RedisVL.Schema;
using RedisVL.Schema.Fields;

namespace RedisVL.Tests;

public class IndexTests
{
    private static IndexSchema CreateTestSchema(string name = "test-idx", string prefix = "test",
        string storageType = "hash")
    {
        var json = $@"{{
            ""index"": {{ ""name"": ""{name}"", ""prefix"": ""{prefix}"", ""storage_type"": ""{storageType}"" }},
            ""fields"": [
                {{ ""name"": ""title"", ""type"": ""text"" }},
                {{ ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": {{ ""dims"": 128 }} }}
            ]
        }}";
        return IndexSchema.FromJson(json);
    }

    [Fact]
    public void SearchIndex_Schema_IsAccessible()
    {
        // SearchIndex requires an IDatabase; we test schema/name via the schema itself
        var schema = CreateTestSchema("my-index", "doc");
        Assert.Equal("my-index", schema.Index.Name);
        Assert.Equal("doc", schema.Index.Prefix);
        Assert.Equal(StorageType.Hash, schema.Index.StorageType);
    }

    [Fact]
    public void SearchIndex_Name_MatchesSchemaName()
    {
        var schema = CreateTestSchema("named-idx");
        Assert.Equal("named-idx", schema.Index.Name);
    }

    [Fact]
    public void SearchIndex_JsonStorageType_ParsedCorrectly()
    {
        var schema = CreateTestSchema("json-idx", "doc", "json");
        Assert.Equal(StorageType.Json, schema.Index.StorageType);
    }

    [Fact]
    public void RedisConnectionProvider_EmptyUrl_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RedisConnectionProvider(""));
    }

    [Fact]
    public void RedisConnectionProvider_NullUrl_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RedisConnectionProvider((string)null!));
    }

    [Fact]
    public void IndexSchema_KeyPrefix_VariousPatterns()
    {
        // Without colon
        var schema1 = CreateTestSchema("idx", "myprefix");
        Assert.Equal("myprefix:", schema1.GetKeyPrefix());

        // With colon
        var json2 = @"{
            ""index"": { ""name"": ""idx"", ""prefix"": ""myprefix:"" },
            ""fields"": []
        }";
        var schema2 = IndexSchema.FromJson(json2);
        Assert.Equal("myprefix:", schema2.GetKeyPrefix());

        // Empty prefix
        var json3 = @"{
            ""index"": { ""name"": ""idx"", ""prefix"": """" },
            ""fields"": []
        }";
        var schema3 = IndexSchema.FromJson(json3);
        Assert.Equal(":", schema3.GetKeyPrefix());
    }

    [Fact]
    public void IndexSchema_FromJson_WithHashStorageType()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"", ""storage_type"": ""hash"" },
            ""fields"": [
                { ""name"": ""content"", ""type"": ""text"" }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        Assert.Equal(StorageType.Hash, schema.Index.StorageType);
    }

    [Fact]
    public void IndexSchema_FromJson_DefaultStorageTypeIsHash()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": []
        }";
        var schema = IndexSchema.FromJson(json);
        Assert.Equal(StorageType.Hash, schema.Index.StorageType);
    }

    [Fact]
    public void IndexSchema_GetVectorField_ReturnsNullWhenNoVectorField()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [
                { ""name"": ""title"", ""type"": ""text"" }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        Assert.Null(schema.GetVectorField());
    }

    [Fact]
    public void IndexSchema_MultipleFields_AllParsed()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [
                { ""name"": ""title"", ""type"": ""text"" },
                { ""name"": ""category"", ""type"": ""tag"" },
                { ""name"": ""price"", ""type"": ""numeric"" },
                { ""name"": ""location"", ""type"": ""geo"" },
                { ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": { ""dims"": 256 } }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        Assert.Equal(5, schema.Fields.Count);
        Assert.IsType<TextField>(schema.Fields[0]);
        Assert.IsType<TagField>(schema.Fields[1]);
        Assert.IsType<NumericField>(schema.Fields[2]);
        Assert.IsType<GeoField>(schema.Fields[3]);
        Assert.IsType<VectorField>(schema.Fields[4]);
    }

    [Fact]
    public void SearchIndex_ConstructorWithNullSchema_Throws()
    {
        // SearchIndex(null, IDatabase) should throw
        Assert.Throws<ArgumentNullException>(() => new SearchIndex(null!, (StackExchange.Redis.IDatabase)null!));
    }

    [Fact]
    public void SearchIndex_ConstructorWithNullDatabase_Throws()
    {
        var schema = CreateTestSchema();
        Assert.Throws<ArgumentNullException>(() => new SearchIndex(schema, (StackExchange.Redis.IDatabase)null!));
    }
}

