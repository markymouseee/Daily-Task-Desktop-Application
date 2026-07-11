using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Services;
using Wpf.Ui.Appearance;

namespace DailyTasks.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly GlobalHotkeyService _hotkeys;
    private readonly GitWatcherService _gitWatcher;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private int _pomodoroMinutes;

    [ObservableProperty]
    private string _recapTimeText;

    [ObservableProperty]
    private double _freeHoursPerDay;

    [ObservableProperty]
    private bool _developerFeaturesEnabled;

    [ObservableProperty]
    private string _gitRepoPath;

    [ObservableProperty]
    private string _gitRepoStatus = string.Empty;

    public SettingsViewModel(SettingsService settings, GlobalHotkeyService hotkeys, GitWatcherService gitWatcher)
    {
        _settings = settings;
        _hotkeys = hotkeys;
        _gitWatcher = gitWatcher;

        _isDarkTheme = settings.Theme == ApplicationTheme.Dark;
        _pomodoroMinutes = settings.PomodoroMinutes;
        _recapTimeText = settings.RecapTime.ToString(@"hh\:mm");
        _freeHoursPerDay = settings.FreeHoursPerDay;
        _developerFeaturesEnabled = settings.DeveloperFeaturesEnabled;
        _gitRepoPath = settings.GitRepoPath ?? string.Empty;

        UpdateGitRepoStatus();
    }

    public string DatabaseLocation => Data.AppDbContext.DatabasePath;

    public string HotkeyStatus => _hotkeys.IsRegistered
        ? $"{GlobalHotkeyService.Gesture} — opens quick capture from anywhere."
        : $"{GlobalHotkeyService.Gesture} is unavailable; another app has claimed it.";

    partial void OnIsDarkThemeChanged(bool value)
    {
        var theme = value ? ApplicationTheme.Dark : ApplicationTheme.Light;

        ApplicationThemeManager.Apply(theme);
        _settings.Theme = theme;
    }

    partial void OnPomodoroMinutesChanged(int value) => _settings.PomodoroMinutes = value;

    partial void OnFreeHoursPerDayChanged(double value) => _settings.FreeHoursPerDay = value;

    partial void OnRecapTimeTextChanged(string value)
    {
        // Only persist input that parses; keep the last valid time otherwise.
        if (TimeSpan.TryParse(value, out var time) && time < TimeSpan.FromDays(1))
        {
            _settings.RecapTime = time;
        }
    }

    partial void OnDeveloperFeaturesEnabledChanged(bool value)
    {
        _settings.DeveloperFeaturesEnabled = value;
        UpdateGitRepoStatus();

        if (value)
        {
            _ = _gitWatcher.PollAsync();
        }
    }

    partial void OnGitRepoPathChanged(string value)
    {
        _settings.GitRepoPath = value;
        UpdateGitRepoStatus();
    }

    private void UpdateGitRepoStatus()
    {
        GitRepoStatus = string.IsNullOrWhiteSpace(GitRepoPath)
            ? "Point this at a local repository to auto-complete tasks from commit messages."
            : GitWatcherService.IsValidRepo(GitRepoPath)
                ? "Watching for commits that mention a task's git link."
                : "That folder doesn't look like a git repository (no .git found).";
    }
}
