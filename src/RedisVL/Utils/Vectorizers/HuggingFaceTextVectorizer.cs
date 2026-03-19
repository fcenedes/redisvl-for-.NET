using System.Net.Http.Headers;
using System.Text.Json;

namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Text vectorizer using HuggingFace's Inference API.
/// </summary>
public class HuggingFaceTextVectorizer : BaseTextVectorizer
{
    private const string DefaultApiUrl = "https://api-inference.huggingface.co/pipeline/feature-extraction";
    private const string DefaultModel = "sentence-transformers/all-MiniLM-L6-v2";
    private readonly string _apiKey;
    private readonly string _apiUrl;
    
    /// <summary>
    /// Creates a HuggingFace text vectorizer.
    /// </summary>
    /// <param name="model">Model name (default: sentence-transformers/all-MiniLM-L6-v2).</param>
    /// <param name="apiKey">API key (defaults to HF_TOKEN env var).</param>
    /// <param name="apiUrl">Custom API endpoint URL.</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public HuggingFaceTextVectorizer(
        string model = DefaultModel,
        string? apiKey = null,
        string? apiUrl = null,
        HttpClient? httpClient = null) : base(httpClient)
    {
        Model = model;
        _apiKey = apiKey ?? GetRequiredEnvVar("HF_TOKEN");
        _apiUrl = apiUrl ?? $"{DefaultApiUrl}/{model}";
        
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
            ["inputs"] = texts,
            ["options"] = new { wait_for_model = true }
        };
        
        using var doc = await PostJsonAsync(_apiUrl, payload);
        var embeddings = new List<float[]>();
        
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var embedding = item.EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
            embeddings.Add(embedding);
            
            if (Dims == 0)
                Dims = embedding.Length;
        }
        
        return embeddings;
    }
}
