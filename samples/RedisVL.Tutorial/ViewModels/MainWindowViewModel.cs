using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace RedisVL.Tutorial.ViewModels;

/// <summary>
/// Main window view model - acts as the root data context with section navigation.
/// </summary>
public partial class MainWindowViewModel : ReactiveObject
{
    [Reactive] private string redisUrl = "redis://localhost:6379";
    [Reactive] private string statusMessage = "Ready. Configure Redis URL and explore the demos.";
    [Reactive] private ReactiveObject? selectedSection;

    public ReadOnlyObservableCollection<ReactiveObject> Sections { get; }

    public MainWindowViewModel(
        SemanticCacheSectionViewModel semanticCache,
        EmbeddingsCacheSectionViewModel embeddingsCache,
        MessageHistorySectionViewModel messageHistory,
        SemanticRouterSectionViewModel semanticRouter)
    {
        var sections = new ObservableCollection<ReactiveObject>(new ReactiveObject[]
        {
            semanticCache,
            embeddingsCache,
            messageHistory,
            semanticRouter
        });

        Sections = new ReadOnlyObservableCollection<ReactiveObject>(sections);
        SelectedSection = semanticCache;
    }
}

