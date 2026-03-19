using RedisVL.Schema;
using RedisVL.Schema.Fields;
using RedisVL.Query;
using RedisVL.Query.Filter;
using RedisVL.Exceptions;

namespace RedisVL.Tests;

public class SchemaTests
{
    [Fact]
    public void IndexSchema_FromYamlString_ParsesCorrectly()
    {
        var yaml = @"
index:
  name: test-idx
  prefix: test
  storage_type: hash
fields:
  - name: title
    type: text
  - name: category
    type: tag
  - name: price
    type: numeric
  - name: location
    type: geo
  - name: embedding
    type: vector
    attrs:
      algorithm: hnsw
      dims: 128
      distance_metric: cosine
";
        var schema = IndexSchema.FromYamlString(yaml);
        
        Assert.Equal("test-idx", schema.Index.Name);
        Assert.Equal("test", schema.Index.Prefix);
        Assert.Equal(StorageType.Hash, schema.Index.StorageType);
        Assert.Equal(5, schema.Fields.Count);
        
        Assert.IsType<TextField>(schema.Fields[0]);
        Assert.Equal("title", schema.Fields[0].Name);
        
        Assert.IsType<TagField>(schema.Fields[1]);
        Assert.Equal("category", schema.Fields[1].Name);
        
        Assert.IsType<NumericField>(schema.Fields[2]);
        Assert.Equal("price", schema.Fields[2].Name);
        
        Assert.IsType<GeoField>(schema.Fields[3]);
        Assert.Equal("location", schema.Fields[3].Name);
        
        var vectorField = Assert.IsType<VectorField>(schema.Fields[4]);
        Assert.Equal("embedding", vectorField.Name);
        Assert.Equal(VectorAlgorithm.HNSW, vectorField.Algorithm);
        Assert.Equal(128, vectorField.Dims);
        Assert.Equal(DistanceMetric.Cosine, vectorField.DistanceMetric);
    }
    
    [Fact]
    public void IndexSchema_FromJson_ParsesCorrectly()
    {
        var json = @"{
            ""index"": { ""name"": ""json-idx"", ""prefix"": ""doc"", ""storage_type"": ""json"" },
            ""fields"": [
                { ""name"": ""content"", ""type"": ""text"" },
                { ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": { ""algorithm"": ""flat"", ""dims"": 256, ""distance_metric"": ""l2"" } }
            ]
        }";
        
        var schema = IndexSchema.FromJson(json);
        
        Assert.Equal("json-idx", schema.Index.Name);
        Assert.Equal(StorageType.Json, schema.Index.StorageType);
        Assert.Equal(2, schema.Fields.Count);
        
        var vectorField = Assert.IsType<VectorField>(schema.Fields[1]);
        Assert.Equal(VectorAlgorithm.Flat, vectorField.Algorithm);
        Assert.Equal(256, vectorField.Dims);
        Assert.Equal(DistanceMetric.L2, vectorField.DistanceMetric);
    }
    
    [Fact]
    public void IndexSchema_GetKeyPrefix_AddsColon()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""myprefix"" },
            ""fields"": []
        }";
        var schema = IndexSchema.FromJson(json);
        Assert.Equal("myprefix:", schema.GetKeyPrefix());
    }
    
    [Fact]
    public void IndexSchema_GetKeyPrefix_NoDoubleColon()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""myprefix:"" },
            ""fields"": []
        }";
        var schema = IndexSchema.FromJson(json);
        Assert.Equal("myprefix:", schema.GetKeyPrefix());
    }
    
    [Fact]
    public void IndexSchema_GetField_ReturnsCorrectField()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [
                { ""name"": ""title"", ""type"": ""text"" },
                { ""name"": ""score"", ""type"": ""numeric"" }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        
        var field = schema.GetField("title");
        Assert.NotNull(field);
        Assert.IsType<TextField>(field);
        
        Assert.Null(schema.GetField("nonexistent"));
    }
    
    [Fact]
    public void IndexSchema_GetVectorField_ReturnsVectorField()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [
                { ""name"": ""title"", ""type"": ""text"" },
                { ""name"": ""embedding"", ""type"": ""vector"", ""attrs"": { ""dims"": 128 } }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        
        var vf = schema.GetVectorField();
        Assert.NotNull(vf);
        Assert.Equal("embedding", vf.Name);
        Assert.Equal(128, vf.Dims);
    }
    
    [Fact]
    public void IndexSchema_UnknownFieldType_Throws()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [{ ""name"": ""unknown"", ""type"": ""xyz"" }]
        }";
        
        Assert.Throws<SchemaValidationException>(() => IndexSchema.FromJson(json));
    }
    
    [Fact]
    public void TextField_Attributes_ParsedCorrectly()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [
                { ""name"": ""title"", ""type"": ""text"", ""attrs"": { ""weight"": 2.0, ""no_stem"": true, ""sortable"": true } }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        var tf = Assert.IsType<TextField>(schema.Fields[0]);
        Assert.Equal(2.0, tf.Weight);
        Assert.True(tf.NoStem);
        Assert.True(tf.Sortable);
    }
    
    [Fact]
    public void TagField_Attributes_ParsedCorrectly()
    {
        var json = @"{
            ""index"": { ""name"": ""test"", ""prefix"": ""doc"" },
            ""fields"": [
                { ""name"": ""cat"", ""type"": ""tag"", ""attrs"": { ""separator"": "";"", ""case_sensitive"": true } }
            ]
        }";
        var schema = IndexSchema.FromJson(json);
        var tgf = Assert.IsType<TagField>(schema.Fields[0]);
        Assert.Equal(";", tgf.Separator);
        Assert.True(tgf.CaseSensitive);
    }
    
    [Fact]
    public void VectorField_DistanceMetricString_Correct()
    {
        var field = new VectorField { DistanceMetric = DistanceMetric.Cosine };
        Assert.Equal("COSINE", field.GetDistanceMetricString());
        
        field.DistanceMetric = DistanceMetric.L2;
        Assert.Equal("L2", field.GetDistanceMetricString());
        
        field.DistanceMetric = DistanceMetric.IP;
        Assert.Equal("IP", field.GetDistanceMetricString());
    }
    
    [Fact]
    public void VectorField_AlgorithmString_Correct()
    {
        var field = new VectorField { Algorithm = VectorAlgorithm.HNSW };
        Assert.Equal("HNSW", field.GetAlgorithmString());
        
        field.Algorithm = VectorAlgorithm.Flat;
        Assert.Equal("FLAT", field.GetAlgorithmString());
    }
    
    [Fact]
    public void FieldBase_FieldPath_DefaultsToJsonPath()
    {
        var field = new TextField { Name = "title" };
        Assert.Equal("$.title", field.FieldPath);
        
        field.Path = "$.custom.path";
        Assert.Equal("$.custom.path", field.FieldPath);
    }
}

public class QueryTests
{
    [Fact]
    public void VectorQuery_GetQueryString_CorrectFormat()
    {
        var query = new VectorQuery(new float[] { 1.0f, 2.0f }, "embedding", 5);
        var qs = query.GetQueryString();
        
        Assert.Equal("(*)=>[KNN 5 @embedding $vec_param AS vector_distance]", qs);
    }
    
    [Fact]
    public void VectorQuery_WithFilter_CorrectFormat()
    {
        var query = new VectorQuery(new float[] { 1.0f }, "embedding", 10)
        {
            FilterExpression = Tag.Field("category") == "electronics"
        };
        var qs = query.GetQueryString();
        
        Assert.Contains("@category:{electronics}", qs);
        Assert.Contains("KNN 10 @embedding", qs);
    }
    
    [Fact]
    public void VectorQuery_WithEfRuntime_IncludesParam()
    {
        var query = new VectorQuery(new float[] { 1.0f }, "embedding", 5)
        {
            EfRuntime = 100
        };
        Assert.Contains("EF_RUNTIME 100", query.GetQueryString());
    }
    
    [Fact]
    public void VectorQuery_GetVectorBytes_CorrectLength()
    {
        var vector = new float[] { 1.0f, 2.0f, 3.0f };
        var query = new VectorQuery(vector, "embedding");
        var bytes = query.GetVectorBytes();
        
        Assert.Equal(3 * sizeof(float), bytes.Length);
    }
    
    [Fact]
    public void RangeQuery_GetQueryString_CorrectFormat()
    {
        var query = new RangeQuery(new float[] { 1.0f }, "embedding", 0.5);
        var qs = query.GetQueryString();
        
        Assert.Contains("VECTOR_RANGE", qs);
        Assert.Contains("0.5", qs);
        Assert.Contains("@embedding", qs);
    }
    
    [Fact]
    public void FilterQuery_GetQueryString_ReturnsFilterOrAll()
    {
        var query = new FilterQuery();
        Assert.Equal("*", query.GetQueryString());
        
        query.FilterExpression = Tag.Field("status") == "active";
        Assert.Contains("@status:{active}", query.GetQueryString());
    }
    
    [Fact]
    public void TextQuery_GetQueryString_WithField()
    {
        var query = new TextQuery("hello world", "content");
        var qs = query.GetQueryString();
        Assert.Equal("@content:(hello world)", qs);
    }
    
    [Fact]
    public void TextQuery_GetQueryString_WithoutField()
    {
        var query = new TextQuery("hello world");
        var qs = query.GetQueryString();
        Assert.Equal("hello world", qs);
    }
    
    [Fact]
    public void TextQuery_GetQueryString_WithFilter()
    {
        var query = new TextQuery("hello", "content")
        {
            FilterExpression = Tag.Field("lang") == "en"
        };
        var qs = query.GetQueryString();
        Assert.Contains("@lang:{en}", qs);
        Assert.Contains("@content:(hello)", qs);
    }
    
    [Fact]
    public void CountQuery_GetQueryString_ReturnsFilterOrAll()
    {
        var query = new CountQuery();
        Assert.Equal("*", query.GetQueryString());
    }
    
    [Fact]
    public void HybridQuery_GetQueryString_CorrectFormat()
    {
        var query = new HybridQuery(new float[] { 1.0f }, "embedding", "search text", "content");
        var qs = query.GetQueryString();
        
        Assert.Contains("KNN", qs);
        Assert.Contains("HYBRID", qs);
        Assert.Contains("@embedding", qs);
    }
    
    [Fact]
    public void SearchResult_GetField_ConvertsType()
    {
        var result = new SearchResult
        {
            Id = "doc:1",
            Fields = new Dictionary<string, object?>
            {
                ["name"] = "test",
                ["count"] = "42"
            }
        };
        
        Assert.Equal("test", result.GetField<string>("name"));
        Assert.Equal("42", result.GetField<string>("count"));
        Assert.Null(result.GetField<string>("nonexistent"));
    }
}

public class FilterTests
{
    [Fact]
    public void TagFilter_Equals_CorrectSyntax()
    {
        var filter = Tag.Field("category") == "electronics";
        Assert.Equal("@category:{electronics}", filter.ToQueryString());
    }
    
    [Fact]
    public void TagFilter_NotEquals_CorrectSyntax()
    {
        var filter = Tag.Field("category") != "electronics";
        Assert.Equal("-@category:{electronics}", filter.ToQueryString());
    }
    
    [Fact]
    public void TagFilter_In_CorrectSyntax()
    {
        var filter = Tag.Field("category").In("electronics", "clothing");
        Assert.Equal("@category:{electronics|clothing}", filter.ToQueryString());
    }
    
    [Fact]
    public void TagFilter_EscapesSpecialChars()
    {
        var filter = Tag.Field("name") == "hello-world";
        Assert.Contains("hello\\-world", filter.ToQueryString());
    }
    
    [Fact]
    public void NumericFilter_GreaterThanOrEqual_CorrectSyntax()
    {
        var filter = Num.Field("price") >= 100;
        Assert.Equal("@price:[100 +inf]", filter.ToQueryString());
    }
    
    [Fact]
    public void NumericFilter_LessThan_CorrectSyntax()
    {
        var filter = Num.Field("price") < 500;
        Assert.Equal("@price:[-inf (500]", filter.ToQueryString());
    }
    
    [Fact]
    public void NumericFilter_Between_CorrectSyntax()
    {
        var filter = Num.Field("price").Between(10, 100);
        Assert.Equal("@price:[10 100]", filter.ToQueryString());
    }
    
    [Fact]
    public void NumericFilter_Equals_CorrectSyntax()
    {
        var filter = Num.Field("age") == 25;
        Assert.Equal("@age:[25 25]", filter.ToQueryString());
    }
    
    [Fact]
    public void AndFilter_CombinesCorrectly()
    {
        var tagFilter = Tag.Field("category") == "electronics";
        var numFilter = Num.Field("price") >= 100;
        var combined = tagFilter & numFilter;
        
        var qs = combined.ToQueryString();
        Assert.Contains("@category:{electronics}", qs);
        Assert.Contains("@price:[100 +inf]", qs);
        Assert.StartsWith("(", qs);
    }
    
    [Fact]
    public void OrFilter_CombinesCorrectly()
    {
        var filter1 = Tag.Field("status") == "active";
        var filter2 = Tag.Field("status") == "pending";
        var combined = filter1 | filter2;
        
        var qs = combined.ToQueryString();
        Assert.Contains("|", qs);
        Assert.Contains("@status:{active}", qs);
        Assert.Contains("@status:{pending}", qs);
    }
    
    [Fact]
    public void NotFilter_NegatesCorrectly()
    {
        var filter = Tag.Field("status") == "deleted";
        var negated = ~filter;
        
        Assert.StartsWith("-", negated.ToQueryString());
    }
    
    [Fact]
    public void GeoFilter_WithinRadius_CorrectSyntax()
    {
        var filter = Geo.Field("location").WithinRadius(-73.935, 40.73, 10, GeoUnit.Kilometers);
        Assert.Equal("@location:[-73.935 40.73 10 km]", filter.ToQueryString());
    }
    
    [Fact]
    public void TextFilter_Match_CorrectSyntax()
    {
        var filter = Text.Field("description").Match("redis database");
        Assert.Equal("@description:(redis database)", filter.ToQueryString());
    }
    
    [Fact]
    public void TextFilter_Like_CorrectSyntax()
    {
        var filter = Text.Field("name").Like("red*");
        Assert.Equal("@name:red*", filter.ToQueryString());
    }
    
    [Fact]
    public void ComplexFilter_CombinesMultipleTypes()
    {
        var tag = Tag.Field("category") == "electronics";
        var price = Num.Field("price") >= 50;
        var text = Text.Field("title").Match("laptop");
        
        var combined = (tag & price) | text;
        var qs = combined.ToQueryString();
        
        Assert.Contains("@category:{electronics}", qs);
        Assert.Contains("@price:", qs);
        Assert.Contains("@title:(laptop)", qs);
    }
}

public class ExceptionTests
{
    [Fact]
    public void RedisVLException_HasMessage()
    {
        var ex = new RedisVLException("test error");
        Assert.Equal("test error", ex.Message);
    }
    
    [Fact]
    public void SchemaValidationException_IsRedisVLException()
    {
        var ex = new SchemaValidationException("invalid schema");
        Assert.IsAssignableFrom<RedisVLException>(ex);
    }
    
    [Fact]
    public void IndexException_HasInnerException()
    {
        var inner = new Exception("inner");
        var ex = new IndexException("index error", inner);
        Assert.Equal("inner", ex.InnerException?.Message);
    }
}

public class VectorizerInterfaceTests
{
    [Fact]
    public void OpenAIVectorizer_RequiresApiKey()
    {
        // Should throw when no env var is set and no key provided
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Assert.Throws<VectorizationException>(() => 
                new RedisVL.Utils.Vectorizers.OpenAITextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }
    }
    
    [Fact]
    public void OpenAIVectorizer_AcceptsExplicitApiKey()
    {
        var vectorizer = new RedisVL.Utils.Vectorizers.OpenAITextVectorizer(apiKey: "test-key");
        Assert.Equal("text-embedding-3-small", vectorizer.Model);
    }
    
    [Fact]
    public void CohereVectorizer_RequiresApiKey()
    {
        var original = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);
            Assert.Throws<VectorizationException>(() => 
                new RedisVL.Utils.Vectorizers.CohereTextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", original);
        }
    }
    
    [Fact]
    public void HuggingFaceVectorizer_RequiresApiKey()
    {
        var original = Environment.GetEnvironmentVariable("HF_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("HF_TOKEN", null);
            Assert.Throws<VectorizationException>(() => 
                new RedisVL.Utils.Vectorizers.HuggingFaceTextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("HF_TOKEN", original);
        }
    }
    
    [Fact]
    public void AzureOpenAIVectorizer_RequiresApiKey()
    {
        var original = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
            Assert.Throws<VectorizationException>(() => 
                new RedisVL.Utils.Vectorizers.AzureOpenAITextVectorizer("deployment", "resource"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", original);
        }
    }
}

public class RerankerInterfaceTests
{
    [Fact]
    public void CohereReranker_RequiresApiKey()
    {
        var original = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);
            Assert.Throws<VectorizationException>(() => 
                new RedisVL.Utils.Rerankers.CohereReranker());
        }
        finally
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", original);
        }
    }
    
    [Fact]
    public void CohereReranker_AcceptsExplicitApiKey()
    {
        var reranker = new RedisVL.Utils.Rerankers.CohereReranker(apiKey: "test-key");
        Assert.Equal("rerank-english-v3.0", reranker.Model);
    }
}

public class RouteTests
{
    [Fact]
    public void Route_HasDefaultThreshold()
    {
        var route = new RedisVL.Extensions.Router.Route { Name = "test" };
        Assert.Equal(0.5, route.DistanceThreshold);
    }
    
    [Fact]
    public void RouteMatch_HasProperties()
    {
        var match = new RedisVL.Extensions.Router.RouteMatch
        {
            Name = "greeting",
            Distance = 0.27
        };
        Assert.Equal("greeting", match.Name);
        Assert.Equal(0.27, match.Distance);
    }
}

public class MessageTests
{
    [Fact]
    public void Message_HasRoleAndContent()
    {
        var msg = new RedisVL.Extensions.MessageHistory.Message
        {
            Role = "user",
            Content = "Hello"
        };
        Assert.Equal("user", msg.Role);
        Assert.Equal("Hello", msg.Content);
    }
    
    [Fact]
    public void Message_SupportsMetadata()
    {
        var msg = new RedisVL.Extensions.MessageHistory.Message
        {
            Role = "llm",
            Content = "Hi",
            Metadata = new Dictionary<string, string> { ["model"] = "gpt-4" }
        };
        Assert.NotNull(msg.Metadata);
        Assert.Equal("gpt-4", msg.Metadata["model"]);
    }
}
