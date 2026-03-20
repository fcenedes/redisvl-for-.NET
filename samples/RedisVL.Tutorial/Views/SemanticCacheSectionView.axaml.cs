using Avalonia.Controls;
using ReactiveUI;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Views;

public partial class SemanticCacheSectionView : UserControl, IViewFor<SemanticCacheSectionViewModel>
{
    public SemanticCacheSectionView()
    {
        InitializeComponent();
    }

    public SemanticCacheSectionViewModel? ViewModel
    {
        get => DataContext as SemanticCacheSectionViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as SemanticCacheSectionViewModel;
    }
}

