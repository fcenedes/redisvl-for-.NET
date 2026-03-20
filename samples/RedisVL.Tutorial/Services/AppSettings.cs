using System.Text.Json.Serialization;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Application settings model, serializable to JSON for persistence.
/// </summary>
public class AppSettings
{
    public string RedisUrl { get; set; } = "redis://localhost:6379";
    public string OpenAiApiKey { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VectorizerMode VectorizerMode { get; set; } = VectorizerMode.Demo;
}

