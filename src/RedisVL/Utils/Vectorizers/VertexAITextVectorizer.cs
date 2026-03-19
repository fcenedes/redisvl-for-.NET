using System.Net.Http.Headers;
using System.Text.Json;

namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Text vectorizer using Google's Vertex AI / Generative Language embedding API.
/// </summary>
public class VertexAITextVectorizer : BaseTextVectorizer
{
    private const string DefaultModel = "textembedding-gecko";
    private const string DefaultApiUrlTemplate = "https://generativelanguage.googleapis.com/v1/models/{0}:embedContent";
    private readonly string _apiKey;
    private readonly string _apiUrl;
    
    /// <summary>
    /// Creates a Vertex AI text vectorizer.
    /// </summary>
    /// <param name="model">Model name (default: textembedding-gecko).</param>
    /// <param name="apiKey">API key (defaults to GOOGLE_API_KEY env var).</param>
    /// <param name="apiUrl">Custom API endpoint URL (overrides the default template).</param>
    /// <param name="dims">Embedding dimensions (0 = model default).</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public VertexAITextVectorizer(
        string model = DefaultModel,
        string? apiKey = null,
        string? apiUrl = null,
        int dims = 0,
        HttpClient? httpClient = null) : base(httpClient)
    {
        Model = model;
        _apiKey = apiKey ?? GetRequiredEnvVar("GOOGLE_API_KEY");
        _apiUrl = apiUrl ?? string.Format(DefaultApiUrlTemplate, model);
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
        var embeddings = new List<float[]>();
        
        // Vertex AI / Generative Language API embeds one text per request
        foreach (var text in texts)
        {
            var payload = new Dictionary<string, object>
            {
                ["content"] = new { parts = new[] { new { text } } }
            };
            
            var url = $"{_apiUrl}?key={_apiKey}";
            
            using var doc = await PostJsonAsync(url, payload);
            var embedding = doc.RootElement.GetProperty("embedding")
                .GetProperty("values")
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

