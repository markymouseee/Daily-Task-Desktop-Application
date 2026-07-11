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

    /// <summary>Display name used for the greeting and the sidebar avatar.</summary>
    public string UserName
    {
        get => _preferences.UserName;
        set
        {
            var trimmed = value?.Trim() ?? string.Empty;

            if (_preferences.UserName == trimmed)
            {
                return;
            }

            _preferences.UserName = trimmed;
            Save();
        }
    }

    /// <summary>
    /// The name to actually show: the user's own if set, otherwise the Windows
    /// account name, falling back to a friendly generic.
    /// </summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(UserName) ? UserName
        : !string.IsNullOrWhiteSpace(Environment.UserName) ? Environment.UserName
        : "there";

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

    public bool DeveloperFeaturesEnabled
    {
        get => _preferences.DeveloperFeaturesEnabled;
        set
        {
            if (_preferences.DeveloperFeaturesEnabled == value)
            {
                return;
            }

            _preferences.DeveloperFeaturesEnabled = value;
            Save();
        }
    }

    /// <summary>Local repository the git watcher polls for task-completing commits.</summary>
    public string? GitRepoPath
    {
        get => _preferences.GitRepoPath;
        set
        {
            var normalised = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

            if (_preferences.GitRepoPath == normalised)
            {
                return;
            }

            _preferences.GitRepoPath = normalised;
            Save();
        }
    }

    /// <summary>Default hours available for tasks each day, for the workload warning.</summary>
    public double FreeHoursPerDay
    {
        get => _preferences.FreeHoursPerDay;
        set
        {
            var clamped = Math.Clamp(value, 0, 24);

            if (Math.Abs(_preferences.FreeHoursPerDay - clamped) < 0.01)
            {
                return;
            }

            _preferences.FreeHoursPerDay = clamped;
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
        public string UserName { get; set; } = string.Empty;

        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Dark;

        public DateTime? LastBigThreePrompt { get; set; }

        public int PomodoroMinutes { get; set; } = 25;

        public TimeSpan RecapTime { get; set; } = new(18, 0, 0);

        public DateTime? LastRecapDate { get; set; }

        public bool DeveloperFeaturesEnabled { get; set; }

        public string? GitRepoPath { get; set; }

        public double FreeHoursPerDay { get; set; } = 6;
    }
}
