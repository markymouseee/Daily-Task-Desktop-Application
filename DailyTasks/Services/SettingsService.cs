using System.IO;
using System.Text.Json;
using Wpf.Ui.Appearance;

namespace DailyTasks.Services;

/// <summary>
/// Persists the handful of user preferences we have to a JSON file next to the database.
/// </summary>
public sealed class SettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyTasks",
        "settings.json");

    private Preferences _preferences = new();

    public ApplicationTheme Theme
    {
        get => _preferences.Theme;
        set
        {
            if (_preferences.Theme == value)
            {
                return;
            }

            _preferences.Theme = value;
            Save();
        }
    }

    /// <summary>The last day the "Big 3" ritual was answered, whether picked or skipped.</summary>
    public DateTime? LastBigThreePrompt
    {
        get => _preferences.LastBigThreePrompt;
        set
        {
            if (_preferences.LastBigThreePrompt == value)
            {
                return;
            }

            _preferences.LastBigThreePrompt = value;
            Save();
        }
    }

    public int PomodoroMinutes
    {
        get => _preferences.PomodoroMinutes;
        set
        {
            var clamped = Math.Clamp(value, 1, 180);

            if (_preferences.PomodoroMinutes == clamped)
            {
                return;
            }

            _preferences.PomodoroMinutes = clamped;
            Save();
        }
    }

    /// <summary>Time of day the end-of-day recap appears.</summary>
    public TimeSpan RecapTime
    {
        get => _preferences.RecapTime;
        set
        {
            if (_preferences.RecapTime == value)
            {
                return;
            }

            _preferences.RecapTime = value;
            Save();
        }
    }

    public DateTime? LastRecapDate
    {
        get => _preferences.LastRecapDate;
        set
        {
            if (_preferences.LastRecapDate == value)
            {
                return;
            }

            _preferences.LastRecapDate = value;
            Save();
        }
    }

    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            _preferences = JsonSerializer.Deserialize<Preferences>(json) ?? new Preferences();
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            _preferences = new Preferences();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_preferences));
    }

    private sealed class Preferences
    {
        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Dark;

        public DateTime? LastBigThreePrompt { get; set; }

        public int PomodoroMinutes { get; set; } = 25;

        public TimeSpan RecapTime { get; set; } = new(18, 0, 0);

        public DateTime? LastRecapDate { get; set; }
    }
}
