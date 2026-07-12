using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Polls a local git repository and completes any open task whose
/// <see cref="TaskItem.GitLink"/> appears in a commit message newer than the task.
/// Runs only while developer features are enabled and a valid repo path is set.
/// </summary>
public sealed class GitWatcherService(ITaskService tasks, SettingsService settings)
{
    private const int CommitScanDepth = 100;

    private readonly DispatcherTimer _timer = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(30),
    };

    private bool _polling;

    /// <summary>Raised after a task was auto-completed by a commit, so open views can react.</summary>
    public event EventHandler<TaskItem>? TaskCompleted;

    public void Start()
    {
        _timer.Tick += async (_, _) => await PollAsync();
        _timer.Start();

        _ = PollAsync();
    }

    public static bool IsValidRepo(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(Path.Combine(path, ".git"));

    public async Task PollAsync()
    {
        if (_polling || !settings.DeveloperFeaturesEnabled || !IsValidRepo(settings.GitRepoPath))
        {
            return;
        }

        _polling = true;

        try
        {
            var roots = await tasks.GetRootsAsync();
            var linked = roots
                .SelectMany(r => TaskTree.Descendants(r).Prepend(r))
                .Where(t => !t.IsCompleted && !string.IsNullOrWhiteSpace(t.GitLink))
                .ToList();

            if (linked.Count == 0)
            {
                return;
            }

            var commits = await ReadRecentCommitsAsync(settings.GitRepoPath!);

            foreach (var task in linked)
            {
                var match = commits.FirstOrDefault(c =>
                    c.Subject.Contains(task.GitLink!, StringComparison.OrdinalIgnoreCase)
                    && c.When >= task.CreatedAt);

                if (match == default)
                {
                    continue;
                }

                task.IsCompleted = true;
                task.CompletedAt = match.When;
                await tasks.CompleteAsync(task);

                TaskCompleted?.Invoke(this, task);
            }
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // A missing git.exe or a repo mid-rebase shouldn't take the app down;
            // the next poll simply tries again.
        }
        finally
        {
            _polling = false;
        }
    }

    private static async Task<IReadOnlyList<(string Hash, DateTime When, string Subject)>> ReadRecentCommitsAsync(string repo)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            ArgumentList = { "-C", repo, "log", $"-{CommitScanDepth}", "--pretty=format:%H%x09%cI%x09%s" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);

        if (process is null)
        {
            return [];
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            return [];
        }

        var commits = new List<(string, DateTime, string)>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', 3);

            if (parts.Length == 3
                && DateTimeOffset.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var when))
            {
                commits.Add((parts[0], when.LocalDateTime, parts[2].Trim()));
            }
        }

        return commits;
    }
}
