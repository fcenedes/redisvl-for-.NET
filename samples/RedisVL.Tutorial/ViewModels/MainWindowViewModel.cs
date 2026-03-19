using CommunityToolkit.Mvvm.ComponentModel;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// Main window view model - acts as the root data context.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _redisUrl = "redis://localhost:6379";

    [ObservableProperty]
    private string _statusMessage = "Ready. Configure Redis URL and explore the demos.";
}

