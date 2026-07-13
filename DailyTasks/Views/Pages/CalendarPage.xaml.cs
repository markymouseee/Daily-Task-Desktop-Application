using System.Windows.Controls;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace DailyTasks.Views.Pages;

public partial class CalendarPage : Page, INavigationAware
{
    private readonly CalendarViewModel _viewModel;

    public CalendarPage(CalendarViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public Task OnNavigatedToAsync() => _viewModel.LoadAsync();

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}
