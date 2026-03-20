using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Extensions.Cache;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

public partial class SemanticCacheSectionViewModel : ReactiveObject, IDisposable
{
    private readonly VectorizerService vectorizerService;
    private readonly LlmService llmService;
    private readonly MetricsService metricsService;
    private readonly CompositeDisposable disposables = new();
    private SemanticCache? cache;

    [Reactive] private string storePrompt = string.Empty;
    [Reactive] private string storeResponse = string.Empty;
    [Reactive] private string checkPrompt = string.Empty;
    [Reactive] private string askPrompt = string.Empty;
    [Reactive] private string output = string.Empty;
    [Reactive] private bool isBusy;
    [Reactive] private double distanceThreshold = 0.3;

    public string Title => "Semantic Cache";

    public ReactiveCommand<Unit, Unit> Store { get; }
    public ReactiveCommand<Unit, Unit> Check { get; }
    public ReactiveCommand<Unit, Unit> Clear { get; }
    public ReactiveCommand<Unit, Unit> Ask { get; }

    public SemanticCacheSectionViewModel(
        VectorizerService vectorizerService,
        LlmService llmService,
        MetricsService metricsService)
    {
        this.vectorizerService = vectorizerService;
        this.llmService = llmService;
        this.metricsService = metricsService;

        RecreateCache();

        disposables.Add(vectorizerService.VectorizerChanged
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => RecreateCache(), ex => Output = $"⚠️ Error: {ex.Message}"));

        disposables.Add(vectorizerService.RedisUrlChanged
            .Skip(1)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ => RecreateCache(), ex => Output = $"⚠️ Error: {ex.Message}"));

        disposables.Add(this.WhenAnyValue(x => x.DistanceThreshold)
            .Skip(1)
            .Subscribe(t => { if (cache != null) cache.DistanceThreshold = t; }));

        var canStore = this.WhenAnyValue(
                x => x.StorePrompt, x => x.IsBusy,
                (prompt, busy) => !string.IsNullOrWhiteSpace(prompt) && !busy);

        var canCheck = this.WhenAnyValue(
                x => x.CheckPrompt, x => x.IsBusy,
                (prompt, busy) => !string.IsNullOrWhiteSpace(prompt) && !busy);

        var canAsk = this.WhenAnyValue(
                x => x.AskPrompt, x => x.IsBusy,
                (prompt, busy) => !string.IsNullOrWhiteSpace(prompt) && !busy);

        var notBusy = this.WhenAnyValue(x => x.IsBusy, busy => !busy);

        Store = ReactiveCommand.CreateFromTask(ExecuteStore, canStore);
        Check = ReactiveCommand.CreateFromTask(ExecuteCheck, canCheck);
        Clear = ReactiveCommand.CreateFromTask(ExecuteClear, notBusy);
        Ask = ReactiveCommand.CreateFromTask(ExecuteAsk, canAsk);

        disposables.Add(Store.ThrownExceptions
            .Merge(Check.ThrownExceptions)
            .Merge(Clear.ThrownExceptions)
            .Merge(Ask.ThrownExceptions)
            .Subscribe(ex =>
            {
                Console.WriteLine($"[{Title}] Error: {ex}");
                Output = $"Error: {ex.Message}" + (ex.InnerException != null ? $"\n  Inner: {ex.InnerException.Message}" : "");
            }));
    }

    private void RecreateCache()
    {
        Console.WriteLine($"[SemanticCache] RecreateCache: mode={vectorizerService.Mode}, url={vectorizerService.RedisUrl}");
        try
        {
            cache?.Dispose();
            cache = new SemanticCache(
                name: "tutorial-cache",
                vectorizer: vectorizerService.CurrentVectorizer,
                redisUrl: vectorizerService.RedisUrl,
                distanceThreshold: DistanceThreshold);
            Console.WriteLine("[SemanticCache] Cache created successfully");
            Output = vectorizerService.Mode == VectorizerMode.Demo
                ? "ℹ️ Demo mode: hash-based vectorizer — only exact text matches.\nSwitch to OpenAI or HuggingFace for true semantic similarity."
                : $"✅ Using {vectorizerService.Mode} vectorizer ({vectorizerService.CurrentDims} dims). Cache recreated.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SemanticCache] RecreateCache failed: {ex}");
            cache = null;
            Output = $"⚠️ Could not connect to Redis: {ex.Message}";
        }
    }

    private async Task ExecuteStore()
    {
        IsBusy = true;
        try
        {
            await cache!.StoreAsync(StorePrompt, StoreResponse);
            Output = $"Stored: \"{StorePrompt}\" → \"{StoreResponse}\"";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteCheck()
    {
        IsBusy = true;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var results = await cache!.CheckAsync(CheckPrompt);
            stopwatch.Stop();

            if (results.Count > 0)
            {
                var savings = metricsService.EstimateSavings();
                metricsService.RecordCacheHit(stopwatch.ElapsedMilliseconds, savings);

                var sb = new StringBuilder();
                sb.AppendLine($"⚡ Cache HIT — {results.Count} result(s), Time: {stopwatch.ElapsedMilliseconds}ms, Saved: ${savings:F4}");
                foreach (var entry in results)
                {
                    sb.AppendLine($"  Prompt:   {entry.Prompt}");
                    sb.AppendLine($"  Response: {entry.Response}");
                    sb.AppendLine($"  Distance: {entry.Distance:F4}");
                    sb.AppendLine();
                }
                Output = sb.ToString().TrimEnd();
            }
            else
            {
                metricsService.RecordCacheMiss(stopwatch.ElapsedMilliseconds);
                Output = $"Cache MISS — no similar prompt found. Time: {stopwatch.ElapsedMilliseconds}ms";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteAsk()
    {
        IsBusy = true;
        try
        {
            Console.WriteLine($"[SemanticCache] Ask: '{AskPrompt}'");
            var stopwatch = Stopwatch.StartNew();

            // Check cache first
            var results = await cache!.CheckAsync(AskPrompt);
            Console.WriteLine($"[SemanticCache] Cache check: {results.Count} results");
            if (results.Count > 0)
            {
                stopwatch.Stop();
                var hit = results[0];
                var savings = metricsService.EstimateSavings();
                metricsService.RecordCacheHit(stopwatch.ElapsedMilliseconds, savings);
                Output = $"⚡ CACHE HIT — Response: {hit.Response}, Distance: {hit.Distance:F4}, Time: {stopwatch.ElapsedMilliseconds}ms, Saved: ${savings:F4}";
            }
            else
            {
                // Cache miss — call OpenAI
                Console.WriteLine("[SemanticCache] Cache MISS, calling LLM...");
                var response = await llmService.Ask(AskPrompt);
                stopwatch.Stop();
                Console.WriteLine($"[SemanticCache] LLM response: {response.Content?.Substring(0, Math.Min(100, response.Content?.Length ?? 0))}...");

                // Cache the response for future queries
                await cache.StoreAsync(AskPrompt, response.Content);

                metricsService.RecordApiCall(response);
                Output = $"🌐 API CALL — Response: {response.Content}, Tokens: {response.TotalTokens} (prompt: {response.PromptTokens}, completion: {response.CompletionTokens}), Cost: ${response.EstimatedCost:F4}, Time: {response.ResponseTimeMs}ms";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteClear()
    {
        IsBusy = true;
        try
        {
            await cache!.ClearAsync();
            Output = "Cache cleared.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose()
    {
        disposables.Dispose();
        cache?.Dispose();
    }
}

