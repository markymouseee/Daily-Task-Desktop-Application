using DailyTasks.ViewModels;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class CommitsWindow : FluentWindow
{
    public CommitsWindow(CommitsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        Loaded += async (_, _) => await viewModel.LoadAsync();
    }
}
