using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RedisVL.Exceptions;

namespace RedisVL.Utils.Vectorizers;

/// <summary>
/// Base class for HTTP-based text vectorizers.
/// </summary>
public abstract class BaseTextVectorizer : ITextVectorizer
{
    protected readonly HttpClient HttpClient;
    
    /// <inheritdoc />
    public string Model { get; protected set; } = string.Empty;
    
    /// <inheritdoc />
    public int Dims { get; protected set; }
    
    protected BaseTextVectorizer(HttpClient? httpClient = null)
    {
        HttpClient = httpClient ?? new HttpClient();
    }
    
    /// <inheritdoc />
    public abstract Task<float[]> EmbedAsync(string text, string? inputType = null);
    
    /// <inheritdoc />
    public abstract Task<IList<float[]>> EmbedManyAsync(IList<string> texts, string? inputType = null);
    
    /// <summary>
    /// Sends a POST request and returns the response as a JsonDocument.
    /// </summary>
    protected async Task<JsonDocument> PostJsonAsync(string url, object payload, 
        Dictionary<string, string>? headers = null)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        if (headers != null)
        {
            foreach (var header in headers)
            {
                HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        
        var response = await HttpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new VectorizationException(
                $"Embedding API request failed with status {response.StatusCode}: {responseBody}");
        }
        
        return JsonDocument.Parse(responseBody);
    }
    
    /// <summary>
    /// Gets an API key from environment variables.
    /// </summary>
    protected static string GetRequiredEnvVar(string name)
    {
        return Environment.GetEnvironmentVariable(name) 
            ?? throw new VectorizationException(
                $"Environment variable '{name}' is not set. Please set it with your API key.");
    }
}
