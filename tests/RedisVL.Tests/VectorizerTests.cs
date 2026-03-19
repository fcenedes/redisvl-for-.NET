using RedisVL.Exceptions;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Tests;

public class ExtendedVectorizerTests
{
    [Fact]
    public void OpenAITextVectorizer_WithCustomModel_UsesSpecifiedModel()
    {
        var vectorizer = new OpenAITextVectorizer(model: "text-embedding-3-large", apiKey: "test-key");
        Assert.Equal("text-embedding-3-large", vectorizer.Model);
    }

    [Fact]
    public void OpenAITextVectorizer_WithCustomDims_SetsCorrectDims()
    {
        var vectorizer = new OpenAITextVectorizer(apiKey: "test-key", dims: 256);
        Assert.Equal(256, vectorizer.Dims);
    }

    [Fact]
    public void OpenAITextVectorizer_WithCustomApiUrl_DoesNotThrow()
    {
        var vectorizer = new OpenAITextVectorizer(
            apiKey: "test-key",
            apiUrl: "https://custom.api.com/v1/embeddings");
        Assert.Equal("text-embedding-3-small", vectorizer.Model);
    }

    [Fact]
    public void OpenAITextVectorizer_DefaultModel_IsCorrect()
    {
        var vectorizer = new OpenAITextVectorizer(apiKey: "test-key");
        Assert.Equal("text-embedding-3-small", vectorizer.Model);
    }

    [Fact]
    public void OpenAITextVectorizer_DefaultDims_IsZero()
    {
        var vectorizer = new OpenAITextVectorizer(apiKey: "test-key");
        Assert.Equal(0, vectorizer.Dims);
    }

    [Fact]
    public void AzureOpenAITextVectorizer_DeploymentAndResource_SetsModel()
    {
        var vectorizer = new AzureOpenAITextVectorizer(
            "my-deployment", "my-resource", apiKey: "test-key");
        Assert.Equal("my-deployment", vectorizer.Model);
    }

    [Fact]
    public void AzureOpenAITextVectorizer_WithDims_SetsCorrectDims()
    {
        var vectorizer = new AzureOpenAITextVectorizer(
            "deployment", "resource", apiKey: "test-key", dims: 512);
        Assert.Equal(512, vectorizer.Dims);
    }

    [Fact]
    public void CohereTextVectorizer_WithCustomModel_UsesSpecifiedModel()
    {
        var vectorizer = new CohereTextVectorizer(model: "embed-multilingual-v3.0", apiKey: "test-key");
        Assert.Equal("embed-multilingual-v3.0", vectorizer.Model);
    }

    [Fact]
    public void CohereTextVectorizer_DefaultModel_IsCorrect()
    {
        var vectorizer = new CohereTextVectorizer(apiKey: "test-key");
        Assert.Equal("embed-english-v3.0", vectorizer.Model);
    }

    [Fact]
    public void HuggingFaceTextVectorizer_WithCustomModel_UsesSpecifiedModel()
    {
        var vectorizer = new HuggingFaceTextVectorizer(
            model: "sentence-transformers/all-mpnet-base-v2", apiKey: "test-key");
        Assert.Equal("sentence-transformers/all-mpnet-base-v2", vectorizer.Model);
    }

    [Fact]
    public void HuggingFaceTextVectorizer_DefaultModel_IsCorrect()
    {
        var vectorizer = new HuggingFaceTextVectorizer(apiKey: "test-key");
        Assert.Equal("sentence-transformers/all-MiniLM-L6-v2", vectorizer.Model);
    }

    [Fact]
    public void OpenAITextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Assert.Throws<VectorizationException>(() => new OpenAITextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }
    }

    [Fact]
    public void CohereTextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("COHERE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", null);
            Assert.Throws<VectorizationException>(() => new CohereTextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("COHERE_API_KEY", original);
        }
    }

    [Fact]
    public void HuggingFaceTextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("HF_TOKEN");
        try
        {
            Environment.SetEnvironmentVariable("HF_TOKEN", null);
            Assert.Throws<VectorizationException>(() => new HuggingFaceTextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("HF_TOKEN", original);
        }
    }

    [Fact]
    public void AzureOpenAITextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
            Assert.Throws<VectorizationException>(() =>
                new AzureOpenAITextVectorizer("deployment", "resource"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", original);
        }
    }

    [Fact]
    public void OpenAITextVectorizer_EnvVarFallback_Works()
    {
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "env-test-key");
            var vectorizer = new OpenAITextVectorizer();
            Assert.Equal("text-embedding-3-small", vectorizer.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }
    }

    // ── VertexAI Vectorizer Tests ──

    [Fact]
    public void VertexAITextVectorizer_DefaultModel_IsCorrect()
    {
        var v = new VertexAITextVectorizer(apiKey: "test-key");
        Assert.Equal("textembedding-gecko", v.Model);
    }

    [Fact]
    public void VertexAITextVectorizer_WithCustomModel_UsesSpecifiedModel()
    {
        var v = new VertexAITextVectorizer(model: "text-embedding-004", apiKey: "test-key");
        Assert.Equal("text-embedding-004", v.Model);
    }

    [Fact]
    public void VertexAITextVectorizer_WithDims_SetsCorrectDims()
    {
        var v = new VertexAITextVectorizer(apiKey: "test-key", dims: 768);
        Assert.Equal(768, v.Dims);
    }

    [Fact]
    public void VertexAITextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", null);
            Assert.Throws<VectorizationException>(() => new VertexAITextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", original);
        }
    }

    [Fact]
    public void VertexAITextVectorizer_EnvVarFallback_Works()
    {
        var original = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", "env-gcp-key");
            var v = new VertexAITextVectorizer();
            Assert.Equal("textembedding-gecko", v.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", original);
        }
    }

    // ── Mistral Vectorizer Tests ──

    [Fact]
    public void MistralTextVectorizer_DefaultModel_IsCorrect()
    {
        var v = new MistralTextVectorizer(apiKey: "test-key");
        Assert.Equal("mistral-embed", v.Model);
    }

    [Fact]
    public void MistralTextVectorizer_WithCustomModel_UsesSpecifiedModel()
    {
        var v = new MistralTextVectorizer(model: "mistral-embed-large", apiKey: "test-key");
        Assert.Equal("mistral-embed-large", v.Model);
    }

    [Fact]
    public void MistralTextVectorizer_WithDims_SetsCorrectDims()
    {
        var v = new MistralTextVectorizer(apiKey: "test-key", dims: 1024);
        Assert.Equal(1024, v.Dims);
    }

    [Fact]
    public void MistralTextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("MISTRAL_API_KEY", null);
            Assert.Throws<VectorizationException>(() => new MistralTextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("MISTRAL_API_KEY", original);
        }
    }

    [Fact]
    public void MistralTextVectorizer_EnvVarFallback_Works()
    {
        var original = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("MISTRAL_API_KEY", "env-mistral-key");
            var v = new MistralTextVectorizer();
            Assert.Equal("mistral-embed", v.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MISTRAL_API_KEY", original);
        }
    }

    // ── VoyageAI Vectorizer Tests ──

    [Fact]
    public void VoyageAITextVectorizer_DefaultModel_IsCorrect()
    {
        var v = new VoyageAITextVectorizer(apiKey: "test-key");
        Assert.Equal("voyage-3-large", v.Model);
    }

    [Fact]
    public void VoyageAITextVectorizer_WithCustomModel_UsesSpecifiedModel()
    {
        var v = new VoyageAITextVectorizer(model: "voyage-3-lite", apiKey: "test-key");
        Assert.Equal("voyage-3-lite", v.Model);
    }

    [Fact]
    public void VoyageAITextVectorizer_WithDims_SetsCorrectDims()
    {
        var v = new VoyageAITextVectorizer(apiKey: "test-key", dims: 512);
        Assert.Equal(512, v.Dims);
    }

    [Fact]
    public void VoyageAITextVectorizer_NullApiKey_NoEnvVar_Throws()
    {
        var original = Environment.GetEnvironmentVariable("VOYAGE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("VOYAGE_API_KEY", null);
            Assert.Throws<VectorizationException>(() => new VoyageAITextVectorizer());
        }
        finally
        {
            Environment.SetEnvironmentVariable("VOYAGE_API_KEY", original);
        }
    }

    [Fact]
    public void VoyageAITextVectorizer_EnvVarFallback_Works()
    {
        var original = Environment.GetEnvironmentVariable("VOYAGE_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("VOYAGE_API_KEY", "env-voyage-key");
            var v = new VoyageAITextVectorizer();
            Assert.Equal("voyage-3-large", v.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VOYAGE_API_KEY", original);
        }
    }

    [Fact]
    public void VoyageAITextVectorizer_DefaultDims_IsZero()
    {
        var v = new VoyageAITextVectorizer(apiKey: "test-key");
        Assert.Equal(0, v.Dims);
    }
}

