using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// ViewModel for the unified settings panel.
/// Manages Redis URL, vectorizer mode, API key, and persistence.
/// </summary>
public partial class SettingsViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly VectorizerService vectorizerService;
    private readonly SettingsService settingsService;

    [Reactive] private VectorizerMode selectedMode;
    [Reactive] private string apiKey = string.Empty;
    [Reactive] private bool showApiKey;
    [Reactive] private string statusMessage = "Demo mode — offline, exact text matching only.";
    [Reactive] private string apiKeyLabel = "API Key";
    [Reactive] private string redisUrl = "redis://localhost:6379";
    [Reactive] private string connectionStatus = "Not tested";
    [Reactive] private bool isConnected;
    [Reactive] private bool isTesting;

    public SettingsViewModel(VectorizerService vectorizerService, SettingsService settingsService)
    {
        this.vectorizerService = vectorizerService;
        this.settingsService = settingsService;

        // Load current values from service
        selectedMode = vectorizerService.Mode;
        apiKey = vectorizerService.ApiKey;
        redisUrl = vectorizerService.RedisUrl;

        AvailableModes = Enum.GetValues<VectorizerMode>();

        disposables.Add(
            this.WhenAnyValue(x => x.SelectedMode)
                .Subscribe(mode =>
                {
                    ShowApiKey = mode != VectorizerMode.Demo;
                    ApiKeyLabel = mode == VectorizerMode.HuggingFace ? "HuggingFace Token" : "API Key";
                    StatusMessage = mode switch
                    {
                        VectorizerMode.Demo => "Demo mode — offline, exact text matching only.",
                        VectorizerMode.OpenAI => "OpenAI mode — true semantic similarity (requires API key).",
                        VectorizerMode.HuggingFace => "HuggingFace mode — true semantic similarity (requires token).",
                        _ => string.Empty
                    };
                }));

        var canSave = this.WhenAnyValue(
            x => x.SelectedMode,
            x => x.ApiKey,
            x => x.RedisUrl,
            x => x.IsTesting,
            (mode, key, url, testing) =>
                !testing &&
                !string.IsNullOrWhiteSpace(url) &&
                (mode == VectorizerMode.Demo || !string.IsNullOrWhiteSpace(key)));

        Save = ReactiveCommand.CreateFromTask(ExecuteSave, canSave);
        disposables.Add(Save);

        disposables.Add(Save.ThrownExceptions
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(ex => ConnectionStatus = $"Error: {ex.Message}"));
    }

    public VectorizerMode[] AvailableModes { get; }

    public ReactiveCommand<Unit, Unit> Save { get; }

    private async Task ExecuteSave()
    {
        // Apply vectorizer settings
        vectorizerService.Mode = SelectedMode;
        vectorizerService.ApiKey = ApiKey;
        vectorizerService.RedisUrl = RedisUrl;

        // Persist to JSON
        settingsService.Save(new AppSettings
        {
            RedisUrl = RedisUrl,
            OpenAiApiKey = ApiKey,
            VectorizerMode = SelectedMode
        });

        // Test Redis connection using RedisConnectionProvider (handles redis:// URL parsing)
        IsTesting = true;
        ConnectionStatus = "Testing connection...";
        try
        {
            using var provider = new RedisVL.Index.RedisConnectionProvider(RedisUrl);
            var db = provider.GetDatabase();
            var pong = await db.PingAsync();
            IsConnected = true;
            ConnectionStatus = $"Connected (ping: {pong.TotalMilliseconds:F0}ms)";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    public void Dispose()
    {
        disposables.Dispose();
    }
}

