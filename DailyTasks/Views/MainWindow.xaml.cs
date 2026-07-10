using DailyTasks.Views.Pages;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(INavigationViewPageProvider pageProvider)
    {
        InitializeComponent();

        RootNavigation.SetPageProviderService(pageProvider);
        Loaded += (_, _) => RootNavigation.Navigate(typeof(TodayPage));
    }
}
