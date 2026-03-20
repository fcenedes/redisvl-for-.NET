using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace RedisVL.Tutorial.Services;

/// <summary>
/// Manages session names across the tutorial app.
/// Sessions are used to group Message History entries and tag Semantic Cache entries.
/// </summary>
public partial class SessionService : ReactiveObject
{
    [Reactive] private string currentSessionName = "session-1";
    
    public ObservableCollection<string> AvailableSessions { get; } = new()
    {
        "session-1"
    };
    
    /// <summary>
    /// Adds a new session and switches to it.
    /// </summary>
    public void AddAndSwitchSession(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var trimmed = name.Trim();
        if (!AvailableSessions.Contains(trimmed))
            AvailableSessions.Add(trimmed);
        CurrentSessionName = trimmed;
    }
}

