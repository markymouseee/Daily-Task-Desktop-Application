using System.Windows;
using System.Windows.Controls;
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
        _viewModel.TodayProjects.CollectionChanged += (_, _) => UpdateSubheading();
    }

    public async Task OnNavigatedToAsync()
    {
        await _viewModel.LoadAsync();
        UpdateHeader();
        QuickAddBox.Focus();
    }

    public Task OnNavigatedFromAsync() => Task.CompletedTask;

    private void OnNewTask(object sender, RoutedEventArgs e) => QuickAddBox.Focus();

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
            DateTime.Now.ToString("dddd, d MMMM"),
            taskCount == 1 ? "1 task" : $"{taskCount} tasks",
        };

        if (_viewModel.TodayProjects.Count > 0)
        {
            parts.Add(_viewModel.TodayProjects.Count == 1 ? "1 project" : $"{_viewModel.TodayProjects.Count} projects");
        }

        SubheadingText.Text = string.Join("  ·  ", parts);
    }

    private static string Greeting() => DateTime.Now.Hour switch
    {
        < 12 => "Good morning",
        < 17 => "Good afternoon",
        _ => "Good evening",
    };
}
