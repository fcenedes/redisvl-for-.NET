using System.Net.Http.Headers;
using System.Text.Json;

namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Text vectorizer using Cohere's embedding API.
/// </summary>
public class CohereTextVectorizer : BaseTextVectorizer
{
    private const string DefaultApiUrl = "https://api.cohere.ai/v1/embed";
    private const string DefaultModel = "embed-english-v3.0";
    private readonly string _apiKey;
    private readonly string _apiUrl;
    
    /// <summary>
    /// Creates a Cohere text vectorizer.
    /// </summary>
    /// <param name="model">Model name (default: embed-english-v3.0).</param>
    /// <param name="apiKey">API key (defaults to COHERE_API_KEY env var).</param>
    /// <param name="apiUrl">Custom API endpoint URL.</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public CohereTextVectorizer(
        string model = DefaultModel,
        string? apiKey = null,
        string apiUrl = DefaultApiUrl,
        HttpClient? httpClient = null) : base(httpClient)
    {
        Model = model;
        _apiKey = apiKey ?? GetRequiredEnvVar("COHERE_API_KEY");
        _apiUrl = apiUrl;
        
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }
    
    /// <inheritdoc />
    public override async Task<float[]> EmbedAsync(string text, string? inputType = null)
    {
        var result = await EmbedManyAsync(new[] { text }, inputType ?? "search_query");
        return result[0];
    }
    
    /// <inheritdoc />
    public override async Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = Model,
            ["texts"] = texts,
            ["input_type"] = inputType ?? "search_document",
            ["embedding_types"] = new[] { "float" }
        };
        
        using var doc = await PostJsonAsync(_apiUrl, payload);
        var embeddings = new List<float[]>();
        
        // Cohere returns embeddings under embeddings.float
        var embeddingsArray = doc.RootElement.GetProperty("embeddings")
            .GetProperty("float");
        
        foreach (var item in embeddingsArray.EnumerateArray())
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
