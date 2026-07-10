using System.Windows.Controls;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace DailyTasks.Views.Pages;

public partial class TodayPage : Page, INavigationAware
{
    private readonly TodayViewModel _viewModel;

    public TodayPage(TodayViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public async Task OnNavigatedToAsync()
    {
        await _viewModel.LoadAsync();
        QuickAddBox.Focus();
    }

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}
