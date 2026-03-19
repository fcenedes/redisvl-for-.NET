using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RedisVL.Extensions.MessageHistory;
using RedisVL.Utils.Vectorizers;

namespace RedisVL.Tutorial.Views;

public partial class MessageHistoryDemo : UserControl
{
    private SemanticMessageHistory? _history;

    public MessageHistoryDemo()
    {
        InitializeComponent();
    }

    private SemanticMessageHistory GetHistory()
    {
        if (_history != null) return _history;

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

        _history = new SemanticMessageHistory(
            name: "tutorial-history",
            vectorizer: vectorizer,
            distanceThreshold: 0.9);

        return _history;
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var roleCombo = this.FindControl<ComboBox>("RoleCombo")!;
            var role = (roleCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "user";
            var content = this.FindControl<TextBox>("MessageContent")!.Text ?? "";
            if (string.IsNullOrWhiteSpace(content)) return;

            await GetHistory().AddMessagesAsync(new[]
            {
                new Message { Role = role, Content = content }
            });

            this.FindControl<TextBox>("Output")!.Text = $"Added message: [{role}] {content}";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var query = this.FindControl<TextBox>("SearchQuery")!.Text ?? "";
            if (string.IsNullOrWhiteSpace(query)) return;

            var results = await GetHistory().GetRelevantAsync(query, topK: 5);
            var sb = new StringBuilder();
            sb.AppendLine($"Found {results.Count} relevant messages for \"{query}\":");
            foreach (var msg in results)
                sb.AppendLine($"  [{msg.Role}] {msg.Content}");

            this.FindControl<TextBox>("Output")!.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnRecentClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var results = await GetHistory().GetRecentAsync(topK: 5);
            var sb = new StringBuilder();
            sb.AppendLine($"Recent {results.Count} messages:");
            foreach (var msg in results)
                sb.AppendLine($"  [{msg.Role}] {msg.Content}");

            this.FindControl<TextBox>("Output")!.Text = sb.ToString();
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
            await GetHistory().ClearAsync();
            this.FindControl<TextBox>("Output")!.Text = "Message history cleared.";
        }
        catch (Exception ex)
        {
            this.FindControl<TextBox>("Output")!.Text = $"Error: {ex.Message}";
        }
    }
}

