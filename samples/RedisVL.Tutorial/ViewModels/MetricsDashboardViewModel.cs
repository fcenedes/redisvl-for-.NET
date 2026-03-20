using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// ViewModel for the metrics dashboard panel.
/// Exposes formatted reactive properties derived from MetricsService.
/// </summary>
public partial class MetricsDashboardViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();

    [Reactive] private string hitRateDisplay = "0%";
    [Reactive] private string savingsDisplay = "$0.00";
    [Reactive] private string apiCostDisplay = "$0.00";
    [Reactive] private string questionsDisplay = "0";
    [Reactive] private string cacheHitsDisplay = "0";
    [Reactive] private string cacheMissesDisplay = "0";
    [Reactive] private string tokensDisplay = "0";
    [Reactive] private string promptTokensDisplay = "0";
    [Reactive] private string completionTokensDisplay = "0";
    [Reactive] private string cacheTimeDisplay = "0ms";
    [Reactive] private string apiTimeDisplay = "0ms";
    [Reactive] private string lastResponseDisplay = "0ms";
    [Reactive] private bool hasData;

    public MetricsService Metrics { get; }

    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    public MetricsDashboardViewModel(MetricsService metrics)
    {
        Metrics = metrics;

        ResetCommand = ReactiveCommand.Create(() => metrics.Reset());
        disposables.Add(ResetCommand);

        disposables.Add(metrics.WhenAnyValue(x => x.CacheHitRate)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => HitRateDisplay = $"{v:F1}%"));

        disposables.Add(metrics.WhenAnyValue(x => x.TotalSavings)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => SavingsDisplay = FormatCost(v)));

        disposables.Add(metrics.WhenAnyValue(x => x.TotalApiCost)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => ApiCostDisplay = FormatCost(v)));

        disposables.Add(metrics.WhenAnyValue(x => x.TotalQuestions)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v =>
            {
                QuestionsDisplay = v.ToString("N0");
                HasData = v > 0;
            }));

        disposables.Add(metrics.WhenAnyValue(x => x.CacheHits)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => CacheHitsDisplay = v.ToString("N0")));

        disposables.Add(metrics.WhenAnyValue(x => x.CacheMisses)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => CacheMissesDisplay = v.ToString("N0")));

        disposables.Add(metrics.WhenAnyValue(x => x.TotalTokensUsed)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => TokensDisplay = v.ToString("N0")));

        disposables.Add(metrics.WhenAnyValue(x => x.TotalPromptTokens)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => PromptTokensDisplay = v.ToString("N0")));

        disposables.Add(metrics.WhenAnyValue(x => x.TotalCompletionTokens)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => CompletionTokensDisplay = v.ToString("N0")));

        disposables.Add(metrics.WhenAnyValue(x => x.AverageCacheTimeMs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => CacheTimeDisplay = $"{v:F0}ms"));

        disposables.Add(metrics.WhenAnyValue(x => x.AverageApiTimeMs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => ApiTimeDisplay = $"{v:F0}ms"));

        disposables.Add(metrics.WhenAnyValue(x => x.LastResponseTimeMs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(v => LastResponseDisplay = $"{v}ms"));
    }

    private static string FormatCost(decimal value)
    {
        return value < 0.01m ? $"${value:F4}" : $"${value:F2}";
    }

    public void Dispose() => disposables.Dispose();
}

