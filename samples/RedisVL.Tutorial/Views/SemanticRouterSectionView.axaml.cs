using Avalonia.Controls;
using ReactiveUI;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Views;

public partial class SemanticRouterSectionView : UserControl, IViewFor<SemanticRouterSectionViewModel>
{
    public SemanticRouterSectionView()
    {
        InitializeComponent();
    }

    public SemanticRouterSectionViewModel? ViewModel
    {
        get => DataContext as SemanticRouterSectionViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as SemanticRouterSectionViewModel;
    }
}

