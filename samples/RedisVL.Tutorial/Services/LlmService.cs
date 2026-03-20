using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Calls OpenAI's chat completion API with real metrics measurement.
/// </summary>
public class LlmService
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string Model = "gpt-4.1-nano";
    private const decimal InputCostPerMillionTokens = 0.10m;
    private const decimal OutputCostPerMillionTokens = 0.40m;

    private readonly SettingsService settingsService;
    private readonly HttpClient httpClient;

    public LlmService(SettingsService settingsService)
    {
        this.settingsService = settingsService;
        httpClient = new HttpClient();
    }

    /// <summary>
    /// Sends a prompt to OpenAI and returns a response with real metrics.
    /// </summary>
    public async Task<LlmResponse> Ask(string prompt)
    {
        var apiKey = settingsService.Settings.OpenAiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Set it in Settings.");
        }

        Console.WriteLine($"[LLM] Calling OpenAI: '{prompt}'");
        var stopwatch = Stopwatch.StartNew();

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.PostAsync(ApiUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[LLM] Error: OpenAI API error ({response.StatusCode}): {responseJson}");
            throw new HttpRequestException($"OpenAI API error ({response.StatusCode}): {responseJson}");
        }

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var messageContent = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var usage = root.GetProperty("usage");
        var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
        var completionTokens = usage.GetProperty("completion_tokens").GetInt32();
        var totalTokens = usage.GetProperty("total_tokens").GetInt32();

        var estimatedCost = (promptTokens * InputCostPerMillionTokens / 1_000_000m)
                          + (completionTokens * OutputCostPerMillionTokens / 1_000_000m);

        Console.WriteLine($"[LLM] Response received: {totalTokens} tokens");

        return new LlmResponse(
            Content: messageContent,
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            TotalTokens: totalTokens,
            EstimatedCost: estimatedCost,
            ResponseTimeMs: stopwatch.ElapsedMilliseconds);
    }
}

