using RedisVL.Exceptions;
using RedisVL.Utils.Rerankers;

namespace RedisVL.Tests;

public class ExtendedRerankerTests
{
    [Fact]
    public void CohereReranker_WithCustomModel_UsesSpecifiedModel()
    {
        var reranker = new CohereReranker(model: "rerank-multilingual-v3.0", apiKey: "test-key");
        Assert.Equal("rerank-multilingual-v3.0", reranker.Model);
    }

    [Fact]
    public void CohereReranker_DefaultModel_IsCorrect()
    {
        var reranker = new CohereReranker(apiKey: "test-key");
        Assert.Equal("rerank-english-v3.0", reranker.Model);
    }

    [Fact]
    public void RerankResult_Properties_CanBeSet()
    {
        var result = new RerankResult
        {
            Index = 2,
            Score = 0.95,
            Content = "Redis is a fast database"
        };

        Assert.Equal(2, result.Index);
        Assert.Equal(0.95, result.Score);
        Assert.Equal("Redis is a fast database", result.Content);
    }

    [Fact]
    public void RerankResult_DefaultValues_AreCorrect()
    {
        var result = new RerankResult();
        Assert.Equal(0, result.Index);
        Assert.Equal(0.0, result.Score);
        Assert.Equal(string.Empty, result.Content);
    }

    [Fact]
    public void CohereReranker_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);
            Assert.Throws<VectorizationException>(() => new CohereReranker());
        }
        finally
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", original);
        }
    }

    [Fact]
    public void CohereReranker_ExplicitApiKey_DoesNotThrow()
    {
        var original = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);
            var reranker = new CohereReranker(apiKey: "explicit-key");
            Assert.Equal("rerank-english-v3.0", reranker.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", original);
        }
    }

    [Fact]
    public void CohereReranker_EnvVarFallback_Works()
    {
        var original = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", "env-test-key");
            var reranker = new CohereReranker();
            Assert.Equal("rerank-english-v3.0", reranker.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", original);
        }
    }
}

