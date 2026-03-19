using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RedisVL.Exceptions;

namespace RedisVL.Utils.Rerankers;

/// <summary>
/// Reranker using Cohere's rerank API.
/// </summary>
public class CohereReranker : IReranker
{
    private const string DefaultApiUrl = "https://api.cohere.ai/v1/rerank";
    private const string DefaultModel = "rerank-english-v3.0";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    
    /// <inheritdoc />
    public string Model { get; }
    
    /// <summary>
    /// Creates a Cohere reranker.
    /// </summary>
    /// <param name="model">Model name (default: rerank-english-v3.0).</param>
    /// <param name="apiKey">API key (defaults to COHERE_API_KEY env var).</param>
    /// <param name="apiUrl">Custom API endpoint URL.</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public CohereReranker(
        string model = DefaultModel,
        string? apiKey = null,
        string apiUrl = DefaultApiUrl,
        HttpClient? httpClient = null)
    {
        Model = model;
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("COHERE_API_KEY")
            ?? throw new VectorizationException("COHERE_API_KEY environment variable is not set.");
        _apiUrl = apiUrl;
        _httpClient = httpClient ?? new HttpClient();
        
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }
    
    /// <inheritdoc />
    public async Task<IList<RerankResult>> RerankAsync(string query, IList<string> documents, int topK = 0)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = Model,
            ["query"] = query,
            ["documents"] = documents
        };
        
        if (topK > 0)
            payload["top_n"] = topK;
        
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(_apiUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new VectorizationException(
                $"Cohere rerank API request failed with status {response.StatusCode}: {responseBody}");
        }
        
        using var doc = JsonDocument.Parse(responseBody);
        var results = new List<RerankResult>();
        
        foreach (var item in doc.RootElement.GetProperty("results").EnumerateArray())
        {
            var index = item.GetProperty("index").GetInt32();
            results.Add(new RerankResult
            {
                Index = index,
                Score = item.GetProperty("relevance_score").GetDouble(),
                Content = documents[index]
            });
        }
        
        return results.OrderByDescending(r => r.Score).ToList();
    }
}
