using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DailyTasks.Services;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions.Controls;

namespace DailyTasks.Views.Pages;

public partial class TodayPage : Page, INavigationAware
{
    private readonly TodayViewModel _viewModel;
    private readonly SettingsService _settings;

    public TodayPage(TodayViewModel viewModel, SettingsService settings)
    {
        _viewModel = viewModel;
        _settings = settings;
        DataContext = viewModel;
        InitializeComponent();

        // Keep the subheading count live as tasks/projects come and go. Reading the
        // view model's existing collections — no view-model changes needed.
        _viewModel.Items.CollectionChanged += (_, _) => UpdateSubheading();
        _viewModel.BigThree.CollectionChanged += (_, _) => UpdateSubheading();
    }

    public async Task OnNavigatedToAsync()
    {
        await _viewModel.LoadAsync();
        UpdateHeader();
        QuickAddBox.Focus();
    }

    public Task OnNavigatedFromAsync() => Task.CompletedTask;

    private void OnKebab(object sender, RoutedEventArgs e)
    {
        if (KebabButton.ContextMenu is { } menu)
        {
            menu.PlacementTarget = KebabButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void OnScrollDown(object sender, RoutedEventArgs e) =>
        TasksScroll.ScrollToVerticalOffset(TasksScroll.VerticalOffset + (TasksScroll.ViewportHeight * 0.9));

    private void OnTasksScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var moreBelow = TasksScroll.ScrollableHeight > 1
            && TasksScroll.VerticalOffset < TasksScroll.ScrollableHeight - 1;

        ScrollDownButton.Visibility = moreBelow ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateHeader()
    {
        GreetingText.Text = $"{Greeting()}, {_settings.DisplayName}";
        UpdateSubheading();
    }

    private void UpdateSubheading()
    {
        var taskCount = _viewModel.Items.Count + _viewModel.BigThree.Count;

        var parts = new List<string>
        {
            DateTime.Now.ToString("dddd, MMMM d"),
            taskCount == 1 ? "1 task today" : $"{taskCount} tasks today",
        };

        SubheadingText.Text = string.Join("  ·  ", parts);
    }

    private static string Greeting() => DateTime.Now.Hour switch
    {
        < 12 => "Good morning",
        < 17 => "Good afternoon",
        _ => "Good evening",
    };
}
