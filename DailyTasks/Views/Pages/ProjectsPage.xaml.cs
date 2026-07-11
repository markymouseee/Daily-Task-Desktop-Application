using System.Windows.Controls;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace DailyTasks.Views.Pages;

public partial class ProjectsPage : Page, INavigationAware
{
    private readonly ProjectsViewModel _viewModel;

    public ProjectsPage(ProjectsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public Task OnNavigatedToAsync() => _viewModel.LoadAsync();

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}
