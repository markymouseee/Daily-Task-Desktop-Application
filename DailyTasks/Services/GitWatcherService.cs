using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>One commit read from a repository, for the feed and for auto-completion matching.</summary>
public sealed record GitCommit(string Hash, DateTime When, string Subject)
{
    public string ShortHash => Hash.Length >= 7 ? Hash[..7] : Hash;
}

/// <summary>
/// Polls local git repositories and completes any open task whose <see cref="TaskItem.GitLink"/>
/// appears in a newer commit. Each project can watch its own repository
/// (<see cref="TaskItem.GitRepoPath"/>); a global repository in settings still applies to every
/// linked task. Runs only while developer features are enabled.
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

    /// <summary>The most recent commits in a repository (newest first), for the commit feed.</summary>
    public Task<IReadOnlyList<GitCommit>> GetRecentCommitsAsync(string? repoPath) =>
        IsValidRepo(repoPath)
            ? ReadRecentCommitsAsync(repoPath!)
            : Task.FromResult<IReadOnlyList<GitCommit>>([]);

    /// <summary>
    /// Scans the relevant repositories once and completes any matching tasks. Returns how many
    /// were completed (0 when disabled or nothing matched) so the Settings "Check now" button
    /// can report a result.
    /// </summary>
    public async Task<int> PollAsync()
    {
        if (_polling || !settings.DeveloperFeaturesEnabled)
        {
            return 0;
        }

        _polling = true;
        var completed = 0;

        try
        {
            var roots = await tasks.GetRootsAsync();

            // Map every task to its project head so we know which repo to scan for it.
            var rootOf = new Dictionary<int, TaskItem>();
            foreach (var root in roots)
            {
                foreach (var task in TaskTree.Descendants(root).Prepend(root))
                {
                    rootOf[task.Id] = root;
                }
            }

            var linked = rootOf.Values.Distinct()
                .SelectMany(r => TaskTree.Descendants(r).Prepend(r))
                .Where(t => !t.IsCompleted && !string.IsNullOrWhiteSpace(t.GitLink))
                .ToList();

            if (linked.Count == 0)
            {
                return 0;
            }

            var cache = new Dictionary<string, IReadOnlyList<GitCommit>>(StringComparer.OrdinalIgnoreCase);

            async Task<IReadOnlyList<GitCommit>> CommitsFor(string? repo)
            {
                if (!IsValidRepo(repo))
                {
                    return [];
                }

                if (!cache.TryGetValue(repo!, out var commits))
                {
                    commits = await ReadRecentCommitsAsync(repo!);
                    cache[repo!] = commits;
                }

                return commits;
            }

            foreach (var task in linked)
            {
                var projectRepo = rootOf.TryGetValue(task.Id, out var head) ? head.GitRepoPath : null;

                // Check the project's own repo first, then the global one.
                foreach (var repo in new[] { projectRepo, settings.GitRepoPath })
                {
                    var match = (await CommitsFor(repo)).FirstOrDefault(c =>
                        c.Subject.Contains(task.GitLink!, StringComparison.OrdinalIgnoreCase)
                        && c.When >= task.CreatedAt);

                    if (match is null)
                    {
                        continue;
                    }

                    task.IsCompleted = true;
                    task.CompletedAt = match.When;
                    await tasks.CompleteAsync(task);

                    TaskCompleted?.Invoke(this, task);
                    completed++;
                    break;
                }
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

        return completed;
    }

    private static async Task<IReadOnlyList<GitCommit>> ReadRecentCommitsAsync(string repo)
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

        var commits = new List<GitCommit>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', 3);

            if (parts.Length == 3
                && DateTimeOffset.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var when))
            {
                commits.Add(new GitCommit(parts[0], when.LocalDateTime, parts[2].Trim()));
            }
        }

        return commits;
    }
}
