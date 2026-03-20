using Avalonia.Controls;
using ReactiveUI;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Views;

public partial class MessageHistorySectionView : UserControl, IViewFor<MessageHistorySectionViewModel>
{
    public MessageHistorySectionView()
    {
        InitializeComponent();
    }

    public MessageHistorySectionViewModel? ViewModel
    {
        get => DataContext as MessageHistorySectionViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as MessageHistorySectionViewModel;
    }
}

