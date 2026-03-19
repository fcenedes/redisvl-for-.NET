using System.Net.Http.Headers;
using System.Text.Json;

namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Text vectorizer using Azure OpenAI's embedding API.
/// </summary>
public class AzureOpenAITextVectorizer : BaseTextVectorizer
{
    private readonly string _apiKey;
    private readonly string _apiUrl;
    
    /// <summary>
    /// Creates an Azure OpenAI text vectorizer.
    /// </summary>
    /// <param name="deploymentName">Azure deployment name.</param>
    /// <param name="resourceName">Azure resource name.</param>
    /// <param name="apiVersion">API version (default: 2024-02-01).</param>
    /// <param name="apiKey">API key (defaults to AZURE_OPENAI_API_KEY env var).</param>
    /// <param name="dims">Embedding dimensions (0 = model default).</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public AzureOpenAITextVectorizer(
        string deploymentName,
        string resourceName,
        string apiVersion = "2024-02-01",
        string? apiKey = null,
        int dims = 0,
        HttpClient? httpClient = null) : base(httpClient)
    {
        Model = deploymentName;
        _apiKey = apiKey ?? GetRequiredEnvVar("AZURE_OPENAI_API_KEY");
        _apiUrl = $"https://{resourceName}.openai.azure.com/openai/deployments/{deploymentName}/embeddings?api-version={apiVersion}";
        Dims = dims;
    }
    
    /// <inheritdoc />
    public override async Task<float[]> EmbedAsync(string text, string? inputType = null)
    {
        var result = await EmbedManyAsync(new[] { text }, inputType);
        return result[0];
    }
    
    /// <inheritdoc />
    public override async Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["input"] = texts
        };
        
        if (Dims > 0)
            payload["dimensions"] = Dims;
        
        var headers = new Dictionary<string, string>
        {
            ["api-key"] = _apiKey
        };
        
        using var doc = await PostJsonAsync(_apiUrl, payload, headers);
        var embeddings = new List<float[]>();
        
        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var embedding = item.GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
            embeddings.Add(embedding);
            
            if (Dims == 0)
                Dims = embedding.Length;
        }
        
        return embeddings;
    }
}
