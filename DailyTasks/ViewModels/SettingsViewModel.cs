using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Services;
using Wpf.Ui.Appearance;

namespace DailyTasks.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly GlobalHotkeyService _hotkeys;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private double _pomodoroMinutes;

    [ObservableProperty]
    private string _recapTimeText;

    public SettingsViewModel(SettingsService settings, GlobalHotkeyService hotkeys)
    {
        _settings = settings;
        _hotkeys = hotkeys;
        _isDarkTheme = settings.Theme == ApplicationTheme.Dark;
        _pomodoroMinutes = settings.PomodoroMinutes;
        _recapTimeText = settings.RecapTime.ToString(@"hh\:mm");
    }

    partial void OnPomodoroMinutesChanged(double value)
    {
        if (value >= 1)
        {
            _settings.PomodoroMinutes = (int)value;
        }
    }

    partial void OnRecapTimeTextChanged(string value)
    {
        // Only accept a complete valid "HH:mm"; keep the last good value otherwise.
        if (TimeSpan.TryParseExact(value, @"hh\:mm", null, out var time))
        {
            _settings.RecapTime = time;
        }
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
}
