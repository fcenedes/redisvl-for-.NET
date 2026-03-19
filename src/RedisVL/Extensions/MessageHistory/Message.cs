using System.Text.Json.Serialization;

namespace RedisVL.Extensions.MessageHistory;

/// <summary>
/// A chat message with role and content.
/// </summary>
public class Message
{
    /// <summary>
    /// The role of the message sender (e.g., "system", "user", "llm", "tool").
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// The message content.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional metadata associated with the message.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
