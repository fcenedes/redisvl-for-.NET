using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Singleton that tracks all metrics from real measurements.
/// Uses ReactiveObject so the dashboard updates live.
/// </summary>
public partial class MetricsService : ReactiveObject
{
    [Reactive] private int totalQuestions;
    [Reactive] private int cacheHits;
    [Reactive] private int cacheMisses;
    [Reactive] private int totalTokensUsed;
    [Reactive] private int totalPromptTokens;
    [Reactive] private int totalCompletionTokens;
    [Reactive] private decimal totalApiCost;
    [Reactive] private decimal totalSavings;
    [Reactive] private long lastResponseTimeMs;
    [Reactive] private double cacheHitRate;
    [Reactive] private double averageCacheTimeMs;
    [Reactive] private double averageApiTimeMs;

    private long sumCacheTimeMs;
    private long sumApiTimeMs;

    /// <summary>
    /// Records a cache hit with real timing data.
    /// </summary>
    public void RecordCacheHit(long responseTimeMs, decimal estimatedSavings)
    {
        TotalQuestions++;
        CacheHits++;
        LastResponseTimeMs = responseTimeMs;
        sumCacheTimeMs += responseTimeMs;
        TotalSavings += estimatedSavings;
        CacheHitRate = TotalQuestions > 0 ? (double)CacheHits / TotalQuestions * 100.0 : 0.0;
        AverageCacheTimeMs = CacheHits > 0 ? (double)sumCacheTimeMs / CacheHits : 0.0;
    }

    /// <summary>
    /// Records an API call with real metrics from the LLM response.
    /// </summary>
    public void RecordApiCall(LlmResponse response)
    {
        TotalQuestions++;
        CacheMisses++;
        TotalTokensUsed += response.TotalTokens;
        TotalPromptTokens += response.PromptTokens;
        TotalCompletionTokens += response.CompletionTokens;
        TotalApiCost += response.EstimatedCost;
        LastResponseTimeMs = response.ResponseTimeMs;
        sumApiTimeMs += response.ResponseTimeMs;
        CacheHitRate = TotalQuestions > 0 ? (double)CacheHits / TotalQuestions * 100.0 : 0.0;
        AverageApiTimeMs = CacheMisses > 0 ? (double)sumApiTimeMs / CacheMisses : 0.0;
    }

    /// <summary>
    /// Estimates the cost savings for a cache hit based on average token usage from previous API calls.
    /// </summary>
    public decimal EstimateSavings()
    {
        if (CacheMisses == 0) return 0m;
        var avgPrompt = TotalPromptTokens / CacheMisses;
        var avgCompletion = TotalCompletionTokens / CacheMisses;
        return (avgPrompt * 0.10m / 1_000_000m) + (avgCompletion * 0.40m / 1_000_000m);
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void Reset()
    {
        TotalQuestions = 0;
        CacheHits = 0;
        CacheMisses = 0;
        TotalTokensUsed = 0;
        TotalPromptTokens = 0;
        TotalCompletionTokens = 0;
        TotalApiCost = 0;
        TotalSavings = 0;
        LastResponseTimeMs = 0;
        CacheHitRate = 0;
        AverageCacheTimeMs = 0;
        AverageApiTimeMs = 0;
        sumCacheTimeMs = 0;
        sumApiTimeMs = 0;
    }
}

