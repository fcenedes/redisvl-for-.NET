using System;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Manages vectorizer creation and configuration. Exposes reactive properties
/// so that demo tabs can observe changes and recreate their caches.
/// </summary>
public partial class VectorizerService : ReactiveObject
{
    private const int DemoDims = 64;
    private const int OpenAIDims = 1536;
    private const int HuggingFaceDims = 384;

    [Reactive] private VectorizerMode mode;
    [Reactive] private string apiKey = string.Empty;
    [Reactive] private string redisUrl = "redis://localhost:6379";

    public VectorizerService()
    {
        mode = VectorizerMode.Demo;
        CurrentVectorizer = CreateVectorizer(VectorizerMode.Demo, string.Empty);

        VectorizerChanged = this.WhenAnyValue(x => x.Mode, x => x.ApiKey)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(t =>
            {
                try
                {
                    return CreateVectorizer(t.Item1, t.Item2);
                }
                catch
                {
                    return CreateDemoVectorizer();
                }
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(v => CurrentVectorizer = v)
            .Select(_ => System.Reactive.Unit.Default);

        RedisUrlChanged = this.WhenAnyValue(x => x.RedisUrl)
            .DistinctUntilChanged();
    }

    public VectorizerService(AppSettings settings) : this()
    {
        mode = settings.VectorizerMode;
        apiKey = settings.OpenAiApiKey;
        redisUrl = settings.RedisUrl;
        CurrentVectorizer = CreateVectorizer(mode, apiKey);
    }

    /// <summary>
    /// The currently active vectorizer instance.
    /// </summary>
    public ITextVectorizer CurrentVectorizer { get; private set; }

    /// <summary>
    /// Observable that fires when the vectorizer configuration changes.
    /// Consumers should subscribe to this to recreate their caches.
    /// </summary>
    public IObservable<System.Reactive.Unit> VectorizerChanged { get; }

    /// <summary>
    /// Observable that fires when the Redis URL changes.
    /// Consumers should subscribe to this to reconnect.
    /// </summary>
    public IObservable<string> RedisUrlChanged { get; }

    /// <summary>
    /// Gets the embedding dimensions for the current mode.
    /// </summary>
    public int CurrentDims => Mode switch
    {
        VectorizerMode.Demo => DemoDims,
        VectorizerMode.OpenAI => OpenAIDims,
        VectorizerMode.HuggingFace => HuggingFaceDims,
        _ => DemoDims
    };

    /// <summary>
    /// Whether the current mode requires an API key.
    /// </summary>
    public bool RequiresApiKey => Mode != VectorizerMode.Demo;

    private static ITextVectorizer CreateVectorizer(VectorizerMode mode, string apiKey)
    {
        return mode switch
        {
            VectorizerMode.OpenAI when !string.IsNullOrWhiteSpace(apiKey) =>
                new OpenAITextVectorizer(
                    model: "text-embedding-3-small",
                    apiKey: apiKey,
                    dims: OpenAIDims),

            VectorizerMode.HuggingFace when !string.IsNullOrWhiteSpace(apiKey) =>
                new HuggingFaceTextVectorizer(
                    model: "sentence-transformers/all-MiniLM-L6-v2",
                    apiKey: apiKey),

            _ => CreateDemoVectorizer()
        };
    }

    private static ITextVectorizer CreateDemoVectorizer()
    {
        return new CustomTextVectorizer(
            embedFunc: text =>
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
                var vector = new float[DemoDims];
                for (var i = 0; i < DemoDims; i++)
                {
                    vector[i] = (hash[i % hash.Length] / 255f) * 2f - 1f;
                }
                return Task.FromResult(vector);
            },
            dims: DemoDims,
            model: "sha256-demo");
    }
}

