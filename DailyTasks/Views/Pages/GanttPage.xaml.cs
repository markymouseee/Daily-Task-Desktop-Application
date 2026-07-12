using System.Windows.Controls;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace DailyTasks.Views.Pages;

public partial class GanttPage : Page, INavigationAware
{
    private readonly GanttPageViewModel _viewModel;

    public GanttPage(GanttPageViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public Task OnNavigatedToAsync() => _viewModel.LoadAsync();

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}
