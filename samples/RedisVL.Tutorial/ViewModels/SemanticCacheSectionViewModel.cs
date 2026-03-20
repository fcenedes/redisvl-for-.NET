using System;
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
    private readonly CompositeDisposable disposables = new();
    private SemanticCache? cache;

    [Reactive] private string storePrompt = string.Empty;
    [Reactive] private string storeResponse = string.Empty;
    [Reactive] private string checkPrompt = string.Empty;
    [Reactive] private string output = string.Empty;
    [Reactive] private bool isBusy;

    public string Title => "Semantic Cache";

    public ReactiveCommand<Unit, Unit> Store { get; }
    public ReactiveCommand<Unit, Unit> Check { get; }
    public ReactiveCommand<Unit, Unit> Clear { get; }

    public SemanticCacheSectionViewModel(VectorizerService vectorizerService)
    {
        this.vectorizerService = vectorizerService;

        RecreateCache();

        disposables.Add(vectorizerService.VectorizerChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => RecreateCache()));

        var canStore = this.WhenAnyValue(
                x => x.StorePrompt, x => x.IsBusy,
                (prompt, busy) => !string.IsNullOrWhiteSpace(prompt) && !busy);

        var canCheck = this.WhenAnyValue(
                x => x.CheckPrompt, x => x.IsBusy,
                (prompt, busy) => !string.IsNullOrWhiteSpace(prompt) && !busy);

        var notBusy = this.WhenAnyValue(x => x.IsBusy, busy => !busy);

        Store = ReactiveCommand.CreateFromTask(ExecuteStore, canStore);
        Check = ReactiveCommand.CreateFromTask(ExecuteCheck, canCheck);
        Clear = ReactiveCommand.CreateFromTask(ExecuteClear, notBusy);

        disposables.Add(Store.ThrownExceptions
            .Merge(Check.ThrownExceptions)
            .Merge(Clear.ThrownExceptions)
            .Subscribe(ex => Output = $"Error: {ex.Message}"));
    }

    private void RecreateCache()
    {
        cache?.Dispose();
        cache = new SemanticCache(
            name: "tutorial-cache",
            vectorizer: vectorizerService.CurrentVectorizer,
            distanceThreshold: 0.3);
        Output = vectorizerService.Mode == VectorizerMode.Demo
            ? "ℹ️ Demo mode: hash-based vectorizer — only exact text matches.\nSwitch to OpenAI or HuggingFace for true semantic similarity."
            : $"✅ Using {vectorizerService.Mode} vectorizer ({vectorizerService.CurrentDims} dims). Cache recreated.";
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
            var results = await cache!.CheckAsync(CheckPrompt);
            if (results.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Cache HIT — {results.Count} result(s):");
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
                Output = "Cache MISS — no similar prompt found.";
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

