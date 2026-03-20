using Avalonia.Controls;
using ReactiveUI;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Views;

public partial class EmbeddingsCacheSectionView : UserControl, IViewFor<EmbeddingsCacheSectionViewModel>
{
    public EmbeddingsCacheSectionView()
    {
        InitializeComponent();
    }

    public EmbeddingsCacheSectionViewModel? ViewModel
    {
        get => DataContext as EmbeddingsCacheSectionViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as EmbeddingsCacheSectionViewModel;
    }
}

