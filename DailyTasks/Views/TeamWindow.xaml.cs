using DailyTasks.ViewModels;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class TeamWindow : FluentWindow
{
    private readonly TeamViewModel _viewModel;

    public TeamWindow(TeamViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += async (_, _) => await _viewModel.LoadAsync();
    }
}
