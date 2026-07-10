using System.Windows.Controls;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace DailyTasks.Views.Pages;

public partial class CompletedPage : Page, INavigationAware
{
    private readonly CompletedViewModel _viewModel;

    public CompletedPage(CompletedViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public Task OnNavigatedToAsync() => _viewModel.LoadAsync();

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}
