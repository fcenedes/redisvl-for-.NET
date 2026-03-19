using System.Net.Http.Headers;
using System.Text.Json;

namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Text vectorizer using OpenAI's embedding API.
/// </summary>
public class OpenAITextVectorizer : BaseTextVectorizer
{
    private const string DefaultApiUrl = "https://api.openai.com/v1/embeddings";
    private const string DefaultModel = "text-embedding-3-small";
    private readonly string _apiKey;
    private readonly string _apiUrl;
    
    /// <summary>
    /// Creates an OpenAI text vectorizer.
    /// </summary>
    /// <param name="model">Model name (default: text-embedding-3-small).</param>
    /// <param name="apiKey">API key (defaults to OPENAI_API_KEY env var).</param>
    /// <param name="apiUrl">Custom API endpoint URL.</param>
    /// <param name="dims">Embedding dimensions (0 = model default).</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public OpenAITextVectorizer(
        string model = DefaultModel, 
        string? apiKey = null,
        string apiUrl = DefaultApiUrl,
        int dims = 0,
        HttpClient? httpClient = null) : base(httpClient)
    {
        Model = model;
        _apiKey = apiKey ?? GetRequiredEnvVar("OPENAI_API_KEY");
        _apiUrl = apiUrl;
        Dims = dims;
        
        HttpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);
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
            ["model"] = Model,
            ["input"] = texts
        };
        
        if (Dims > 0)
            payload["dimensions"] = Dims;
        
        using var doc = await PostJsonAsync(_apiUrl, payload);
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
