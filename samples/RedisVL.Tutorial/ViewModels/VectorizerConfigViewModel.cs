using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// ViewModel for the vectorizer configuration settings panel.
/// Allows users to switch between Demo, OpenAI, and HuggingFace modes.
/// </summary>
public partial class VectorizerConfigViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly VectorizerService vectorizerService;

    [Reactive] private VectorizerMode selectedMode;
    [Reactive] private string apiKey = string.Empty;
    [Reactive] private bool showApiKey;
    [Reactive] private string statusMessage = "Demo mode — offline, exact text matching only.";
    [Reactive] private string apiKeyLabel = "API Key";

    public VectorizerConfigViewModel(VectorizerService vectorizerService)
    {
        this.vectorizerService = vectorizerService;
        selectedMode = vectorizerService.Mode;
        apiKey = vectorizerService.ApiKey;

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

        var canApply = this.WhenAnyValue(
            x => x.SelectedMode,
            x => x.ApiKey,
            (mode, key) => mode == VectorizerMode.Demo || !string.IsNullOrWhiteSpace(key));

        Apply = ReactiveCommand.Create(ApplySettings, canApply);
        disposables.Add(Apply);
    }

    public VectorizerMode[] AvailableModes { get; }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> Apply { get; }

    private void ApplySettings()
    {
        vectorizerService.Mode = SelectedMode;
        vectorizerService.ApiKey = ApiKey;
    }

    public void Dispose()
    {
        disposables.Dispose();
    }
}

