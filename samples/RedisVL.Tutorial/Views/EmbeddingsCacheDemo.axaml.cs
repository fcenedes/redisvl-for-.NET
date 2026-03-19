using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RedisVL.Extensions.Cache;

namespace RedisVL.Tutorial.Views;

public partial class EmbeddingsCacheDemo : UserControl
{
    private EmbeddingsCache? _cache;

    public EmbeddingsCacheDemo()
    {
        InitializeComponent();
    }

    private EmbeddingsCache GetCache()
        => _cache ??= new EmbeddingsCache(prefix: "tutorial-emb");

    private static float[] DemoEmbed(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToLowerInvariant().Trim()));
        var floats = new float[32];
        for (int i = 0; i < 32; i++)
            floats[i] = (hash[i % hash.Length] - 128f) / 128f;
        return floats;
    }

    private async void OnSetClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = this.FindControl<TextBox>("EmbText")!.Text ?? "";
            var model = this.FindControl<TextBox>("EmbModel")!.Text ?? "demo-model";
            if (string.IsNullOrWhiteSpace(text)) return;

            var embedding = DemoEmbed(text);
            var key = await GetCache().SetAsync(text, model, embedding);
            this.FindControl<TextBox>("Output")!.Text =
                $"Stored embedding for \"{text}\" (model: {model})\nKey: {key}\nDims: {embedding.Length}";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnGetClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = this.FindControl<TextBox>("GetText")!.Text ?? "";
            var model = this.FindControl<TextBox>("EmbModel")!.Text ?? "demo-model";
            if (string.IsNullOrWhiteSpace(text)) return;

            var entry = await GetCache().GetAsync(text, model);
            this.FindControl<TextBox>("Output")!.Text = entry != null
                ? $"Found: text=\"{entry.Text}\", model=\"{entry.ModelName}\", dims={entry.Embedding.Length}\nFirst 5 values: [{string.Join(", ", entry.Embedding.Take(5).Select(f => f.ToString("F4")))}...]"
                : "Not found.";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnExistsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = this.FindControl<TextBox>("GetText")!.Text ?? "";
            var model = this.FindControl<TextBox>("EmbModel")!.Text ?? "demo-model";
            var exists = await GetCache().ExistsAsync(text, model);
            this.FindControl<TextBox>("Output")!.Text = $"Exists(\"{text}\", \"{model}\"): {exists}";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnDropClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var text = this.FindControl<TextBox>("GetText")!.Text ?? "";
            var model = this.FindControl<TextBox>("EmbModel")!.Text ?? "demo-model";
            await GetCache().DropAsync(text, model);
            this.FindControl<TextBox>("Output")!.Text = $"Dropped entry for \"{text}\".";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnMSetClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var raw = this.FindControl<TextBox>("BatchTexts")!.Text ?? "";
            var model = this.FindControl<TextBox>("EmbModel")!.Text ?? "demo-model";
            var texts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var embeddings = texts.Select(DemoEmbed).ToList();

            var keys = await GetCache().MSetAsync(texts, model, embeddings);
            this.FindControl<TextBox>("Output")!.Text = $"MSet stored {keys.Count} embeddings.\nKeys:\n{string.Join("\n", keys)}";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnMGetClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var raw = this.FindControl<TextBox>("BatchTexts")!.Text ?? "";
            var model = this.FindControl<TextBox>("EmbModel")!.Text ?? "demo-model";
            var texts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var results = await GetCache().MGetAsync(texts, model);
            var sb = new StringBuilder();
            for (int i = 0; i < texts.Count; i++)
            {
                var r = results[i];
                sb.AppendLine(r != null ? $"  {texts[i]}: dims={r.Embedding.Length}" : $"  {texts[i]}: NOT FOUND");
            }
            this.FindControl<TextBox>("Output")!.Text = $"MGet results:\n{sb}";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }



    private async void OnClearClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await GetCache().ClearAsync();
            this.FindControl<TextBox>("Output")!.Text = "All embeddings cache entries cleared.";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }
}