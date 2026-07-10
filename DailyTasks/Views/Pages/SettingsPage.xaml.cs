using System.Windows.Controls;
using DailyTasks.ViewModels;

namespace DailyTasks.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
