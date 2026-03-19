using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RedisVL.Extensions.Cache;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Tutorial.Views;

public partial class SemanticCacheDemo : UserControl
{
    private SemanticCache? _cache;

    public SemanticCacheDemo()
    {
        InitializeComponent();
    }

    private SemanticCache GetCache()
    {
        if (_cache != null) return _cache;

        // Demo vectorizer: deterministic hash-based embedding (not real semantic similarity)
        var vectorizer = new CustomTextVectorizer(
            embedFunc: text =>
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToLowerInvariant().Trim()));
                var floats = new float[64];
                for (int i = 0; i < 64; i++)
                    floats[i] = (hash[i % hash.Length] - 128f) / 128f;
                return Task.FromResult(floats);
            },
            dims: 64,
            model: "demo-hash-vectorizer");

        _cache = new SemanticCache(
            name: "tutorial-cache",
            vectorizer: vectorizer,
            distanceThreshold: 0.3);

        return _cache;
    }

    private async void OnStoreClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var prompt = this.FindControl<TextBox>("StorePrompt")!.Text ?? "";
            var response = this.FindControl<TextBox>("StoreResponse")!.Text ?? "";
            if (string.IsNullOrWhiteSpace(prompt)) return;

            await GetCache().StoreAsync(prompt, response);
            this.FindControl<TextBox>("Output")!.Text = $"Stored: \"{prompt}\" → \"{response}\"";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnCheckClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var prompt = this.FindControl<TextBox>("CheckPrompt")!.Text ?? "";
            if (string.IsNullOrWhiteSpace(prompt)) return;

            var results = await GetCache().CheckAsync(prompt);
            if (results.Count > 0)
            {
                var entry = results[0];
                this.FindControl<TextBox>("Output")!.Text =
                    $"Cache HIT!\nPrompt: {entry.Prompt}\nResponse: {entry.Response}\nDistance: {entry.Distance:F4}";
            }
            else
            {
                this.FindControl<TextBox>("Output")!.Text = "Cache MISS - no similar prompt found.";
            }
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
            this.FindControl<TextBox>("Output")!.Text = "Cache cleared.";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }
}

