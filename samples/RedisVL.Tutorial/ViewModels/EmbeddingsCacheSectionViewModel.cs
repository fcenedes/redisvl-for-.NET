using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Extensions.Cache;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

public partial class EmbeddingsCacheSectionViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly VectorizerService vectorizerService;
    private EmbeddingsCache cache;

    [Reactive] private string text = string.Empty;
    [Reactive] private string modelName = "demo-model";
    [Reactive] private string batchTexts = string.Empty;
    [Reactive] private string output = string.Empty;

    public EmbeddingsCacheSectionViewModel(VectorizerService vectorizerService)
    {
        this.vectorizerService = vectorizerService;
        try
        {
            cache = new EmbeddingsCache(redisUrl: vectorizerService.RedisUrl, prefix: "tutorial-emb");
        }
        catch (Exception ex)
        {
            cache = null!;
            Output = $"⚠️ Could not connect to Redis: {ex.Message}";
        }

        disposables.Add(vectorizerService.RedisUrlChanged
            .Skip(1)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                try
                {
                    cache?.Dispose();
                    cache = new EmbeddingsCache(redisUrl: vectorizerService.RedisUrl, prefix: "tutorial-emb");
                    Output = "Redis URL changed — embeddings cache reconnected.";
                }
                catch (Exception ex)
                {
                    cache = null!;
                    Output = $"⚠️ Could not connect to Redis: {ex.Message}";
                }
            }, ex => Output = $"⚠️ Error: {ex.Message}"));

        var canExecuteSingle = this.WhenAnyValue(x => x.Text, t => !string.IsNullOrWhiteSpace(t));
        var canExecuteBatch = this.WhenAnyValue(x => x.BatchTexts, t => !string.IsNullOrWhiteSpace(t));

        Set = ReactiveCommand.CreateFromTask(ExecuteSet, canExecuteSingle);
        Get = ReactiveCommand.CreateFromTask(ExecuteGet, canExecuteSingle);
        Exists = ReactiveCommand.CreateFromTask(ExecuteExists, canExecuteSingle);
        Drop = ReactiveCommand.CreateFromTask(ExecuteDrop, canExecuteSingle);
        MSet = ReactiveCommand.CreateFromTask(ExecuteMSet, canExecuteBatch);
        MGet = ReactiveCommand.CreateFromTask(ExecuteMGet, canExecuteBatch);
        Clear = ReactiveCommand.CreateFromTask(ExecuteClear);

        disposables.Add(
            Set.ThrownExceptions
                .Merge(Get.ThrownExceptions)
                .Merge(Exists.ThrownExceptions)
                .Merge(Drop.ThrownExceptions)
                .Merge(MSet.ThrownExceptions)
                .Merge(MGet.ThrownExceptions)
                .Merge(Clear.ThrownExceptions)
                .Subscribe(ex =>
                {
                    Console.WriteLine($"[{Title}] Error: {ex}");
                    Output = $"Error: {ex.Message}" + (ex.InnerException != null ? $"\n  Inner: {ex.InnerException.Message}" : "");
                }));
    }

    public string Title => "Embeddings Cache";

    public ReactiveCommand<Unit, Unit> Set { get; }
    public ReactiveCommand<Unit, Unit> Get { get; }
    public ReactiveCommand<Unit, Unit> Exists { get; }
    public ReactiveCommand<Unit, Unit> Drop { get; }
    public ReactiveCommand<Unit, Unit> MSet { get; }
    public ReactiveCommand<Unit, Unit> MGet { get; }
    public ReactiveCommand<Unit, Unit> Clear { get; }

    private async Task ExecuteSet()
    {
        var embedding = await vectorizerService.CurrentVectorizer.EmbedAsync(Text);
        var model = string.IsNullOrWhiteSpace(ModelName) ? vectorizerService.CurrentVectorizer.Model : ModelName;
        var key = await cache.SetAsync(Text, model, embedding);
        Output = $"Stored embedding for \"{Text}\" (model: {model})\n" +
                 $"Key: {key}\n" +
                 $"Dims: {embedding.Length}\n" +
                 $"First 5 values: [{string.Join(", ", embedding.Take(5).Select(f => f.ToString("F4")))}…]";
    }

    private async Task ExecuteGet()
    {
        var model = string.IsNullOrWhiteSpace(ModelName) ? vectorizerService.CurrentVectorizer.Model : ModelName;
        var entry = await cache.GetAsync(Text, model);
        if (entry == null)
        {
            Output = "Not found.";
            return;
        }

        Output = $"Found: text=\"{entry.Text}\", model=\"{entry.ModelName}\"\n" +
                 $"Dims: {entry.Embedding.Length}\n" +
                 $"First 5 values: [{string.Join(", ", entry.Embedding.Take(5).Select(f => f.ToString("F4")))}…]";
    }

    private async Task ExecuteExists()
    {
        var model = string.IsNullOrWhiteSpace(ModelName) ? vectorizerService.CurrentVectorizer.Model : ModelName;
        var exists = await cache.ExistsAsync(Text, model);
        Output = $"Exists(\"{Text}\", \"{model}\"): {exists}";
    }

    private async Task ExecuteDrop()
    {
        var model = string.IsNullOrWhiteSpace(ModelName) ? vectorizerService.CurrentVectorizer.Model : ModelName;
        await cache.DropAsync(Text, model);
        Output = $"Dropped entry for \"{Text}\" (model: {model}).";
    }

    private async Task ExecuteMSet()
    {
        var texts = BatchTexts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var model = string.IsNullOrWhiteSpace(ModelName) ? vectorizerService.CurrentVectorizer.Model : ModelName;
        var embeddings = await vectorizerService.CurrentVectorizer.EmbedManyAsync(texts);
        var keys = await cache.MSetAsync(texts, model, embeddings.ToList());

        var sb = new StringBuilder();
        sb.AppendLine($"MSet stored {keys.Count} embeddings (model: {model}):");
        foreach (var key in keys)
            sb.AppendLine($"  {key}");
        Output = sb.ToString().TrimEnd();
    }

    private async Task ExecuteMGet()
    {
        var texts = BatchTexts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var model = string.IsNullOrWhiteSpace(ModelName) ? vectorizerService.CurrentVectorizer.Model : ModelName;
        var results = await cache.MGetAsync(texts, model);

        var sb = new StringBuilder();
        sb.AppendLine("MGet results:");
        for (var i = 0; i < texts.Count; i++)
        {
            var r = results[i];
            sb.AppendLine(r != null
                ? $"  {texts[i]}: dims={r.Embedding.Length}, first 5=[{string.Join(", ", r.Embedding.Take(5).Select(f => f.ToString("F4")))}…]"
                : $"  {texts[i]}: NOT FOUND");
        }
        Output = sb.ToString().TrimEnd();
    }

    private async Task ExecuteClear()
    {
        await cache.ClearAsync();
        Output = "All embeddings cache entries cleared.";
    }

    public void Dispose()
    {
        disposables.Dispose();
        cache.Dispose();
    }
}

