namespace RedisVL.Tutorial.Services;

/// <summary>
/// Represents a response from the OpenAI chat completion API with real metrics.
/// </summary>
public record LlmResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCost,
    long ResponseTimeMs);

