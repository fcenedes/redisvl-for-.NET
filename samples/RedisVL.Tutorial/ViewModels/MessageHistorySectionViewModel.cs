using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RedisVL.Extensions.MessageHistory;
using RedisVL.Tutorial.Services;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// ViewModel for the Message History demo section.
/// Demonstrates SemanticMessageHistory with add, recent, search, and clear operations.
/// </summary>
public partial class MessageHistorySectionViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly VectorizerService vectorizerService;
    private SemanticMessageHistory? history;

    [Reactive] private string selectedRole = "user";
    [Reactive] private string messageContent = string.Empty;
    [Reactive] private string searchQuery = string.Empty;
    [Reactive] private string output = string.Empty;

    public SessionService SessionService { get; }

    public MessageHistorySectionViewModel(VectorizerService vectorizerService, SessionService sessionService)
    {
        this.vectorizerService = vectorizerService;
        SessionService = sessionService;

        AvailableRoles = new[] { "user", "assistant", "system" };

        // Recreate history when vectorizer or Redis URL changes
        disposables.Add(
            vectorizerService.VectorizerChanged
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ => RecreateHistory(), ex => Output = $"⚠️ Error: {ex.Message}"));

        disposables.Add(
            vectorizerService.RedisUrlChanged
                .Skip(1)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ => RecreateHistory(), ex => Output = $"⚠️ Error: {ex.Message}"));

        // Recreate history when session changes and auto-load recent
        disposables.Add(
            sessionService.WhenAnyValue(x => x.CurrentSessionName)
                .Skip(1)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    RecreateHistory();
                    // Auto-load recent messages for the new session
                    GetRecent?.Execute().Subscribe(_ => { }, _ => { });
                }));

        var canAddMessage = this.WhenAnyValue(x => x.MessageContent,
            content => !string.IsNullOrWhiteSpace(content));

        var canSearch = this.WhenAnyValue(x => x.SearchQuery,
            query => !string.IsNullOrWhiteSpace(query));

        AddMessage = ReactiveCommand.CreateFromTask(ExecuteAddMessage, canAddMessage);
        GetRecent = ReactiveCommand.CreateFromTask(ExecuteGetRecent);
        SearchRelevant = ReactiveCommand.CreateFromTask(ExecuteSearchRelevant, canSearch);
        Clear = ReactiveCommand.CreateFromTask(ExecuteClear);

        disposables.Add(
            AddMessage.ThrownExceptions
                .Merge(GetRecent.ThrownExceptions)
                .Merge(SearchRelevant.ThrownExceptions)
                .Merge(Clear.ThrownExceptions)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(ex =>
                {
                    Console.WriteLine($"[{Title}] Error: {ex}");
                    Output = $"Error: {ex.Message}" + (ex.InnerException != null ? $"\n  Inner: {ex.InnerException.Message}" : "");
                }));

        disposables.Add(AddMessage);
        disposables.Add(GetRecent);
        disposables.Add(SearchRelevant);
        disposables.Add(Clear);
    }

    public string Title => "Message History";

    public string[] AvailableRoles { get; }

    public ReactiveCommand<Unit, Unit> AddMessage { get; }
    public ReactiveCommand<Unit, Unit> GetRecent { get; }
    public ReactiveCommand<Unit, Unit> SearchRelevant { get; }
    public ReactiveCommand<Unit, Unit> Clear { get; }

    private SemanticMessageHistory GetHistory()
    {
        if (history != null) return history;

        try
        {
            history = new SemanticMessageHistory(
                name: SessionService.CurrentSessionName,
                vectorizer: vectorizerService.CurrentVectorizer,
                redisUrl: vectorizerService.RedisUrl,
                distanceThreshold: 0.9);
        }
        catch (Exception ex)
        {
            Output = $"⚠️ Could not connect to Redis: {ex.Message}";
            throw;
        }

        return history;
    }

    private void RecreateHistory()
    {
        history?.Dispose();
        history = null;
        Output = "Vectorizer changed — history instance will be recreated on next operation.";
    }

    private async Task ExecuteAddMessage()
    {
        var role = SelectedRole;
        var content = MessageContent;

        await GetHistory().AddMessagesAsync(new[]
        {
            new Message { Role = role, Content = content }
        });

        MessageContent = string.Empty;

        // Auto-refresh to show updated history
        await ExecuteGetRecent();
    }

    private async Task ExecuteGetRecent()
    {
        var results = await GetHistory().GetRecentAsync(topK: 5);
        var sb = new StringBuilder();
        sb.AppendLine($"Recent {results.Count} messages:");
        foreach (var msg in results)
            sb.AppendLine($"  [{msg.Role}] {msg.Content}");

        Output = sb.ToString();
    }

    private async Task ExecuteSearchRelevant()
    {
        var query = SearchQuery;
        var results = await GetHistory().GetRelevantAsync(query, topK: 5);
        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} relevant messages for \"{query}\":");
        foreach (var msg in results)
            sb.AppendLine($"  [{msg.Role}] {msg.Content}");

        Output = sb.ToString();
    }

    private async Task ExecuteClear()
    {
        await GetHistory().ClearAsync();
        Output = "Message history cleared.";
    }

    public void Dispose()
    {
        history?.Dispose();
        disposables.Dispose();
    }
}

