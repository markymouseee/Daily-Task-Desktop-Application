using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// A project's git repository: set the path it watches, and see every recent commit — not just
/// the ones that closed a task. Commits mentioning a subtask's git link still auto-complete it.
/// </summary>
public partial class CommitsViewModel : ObservableObject
{
    private readonly TaskItem _project;
    private readonly GitWatcherService _git;
    private readonly ITaskService _tasks;

    [ObservableProperty]
    private string _repoPath;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty = true;

    public CommitsViewModel(TaskItem project, GitWatcherService git, ITaskService tasks)
    {
        _project = project;
        _git = git;
        _tasks = tasks;
        _repoPath = project.GitRepoPath ?? string.Empty;
        UpdateStatus();
    }

    public string ProjectTitle => _project.Title;

    public ObservableCollection<GitCommit> Commits { get; } = [];

    public Task LoadAsync() => LoadCommitsAsync();

    partial void OnRepoPathChanged(string value) => UpdateStatus();

    /// <summary>Persist the project's repo path, then reload its commits.</summary>
    [RelayCommand]
    private async Task Save()
    {
        _project.GitRepoPath = string.IsNullOrWhiteSpace(RepoPath) ? null : RepoPath.Trim();
        await _tasks.UpdateAsync(_project);
        UpdateStatus();
        await LoadCommitsAsync();
    }

    [RelayCommand]
    private Task Refresh() => LoadCommitsAsync();

    private async Task LoadCommitsAsync()
    {
        Commits.Clear();

        foreach (var commit in await _git.GetRecentCommitsAsync(RepoPath))
        {
            Commits.Add(commit);
        }

        IsEmpty = Commits.Count == 0;
    }

    private void UpdateStatus()
    {
        StatusText = string.IsNullOrWhiteSpace(RepoPath)
            ? "Point this project at a local repository to track its commits and auto-complete its subtasks."
            : GitWatcherService.IsValidRepo(RepoPath)
                ? "Watching this repository. A commit that mentions a subtask's git link closes it."
                : "That folder doesn't look like a git repository (no .git found).";
    }
}
