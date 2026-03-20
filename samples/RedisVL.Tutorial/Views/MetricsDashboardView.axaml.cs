using Avalonia.Controls;
using ReactiveUI;
using RedisVL.Tutorial.ViewModels;

namespace RedisVL.Tutorial.Views;

public partial class MetricsDashboardView : UserControl, IViewFor<MetricsDashboardViewModel>
{
    public MetricsDashboardView()
    {
        InitializeComponent();
    }

    public MetricsDashboardViewModel? ViewModel
    {
        get => DataContext as MetricsDashboardViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as MetricsDashboardViewModel;
    }
}

