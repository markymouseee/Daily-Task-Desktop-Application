using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// Shared behaviour for the Today / All Tasks / Completed pages. Subclasses decide
/// which tasks they show via <see cref="Includes"/>; grouping and sorting are left
/// to each page's XAML.
/// </summary>
public abstract partial class TaskListViewModel(ITaskService tasks, FocusService focus, ITaskEditor editor)
    : ObservableObject
{
    [ObservableProperty]
    private bool _isEmpty = true;

    protected ITaskService Tasks { get; } = tasks;

    protected FocusService Focus { get; } = focus;

    public ObservableCollection<TaskItemViewModel> Items { get; } = [];

    /// <summary>Shown when the page has no tasks to display.</summary>
    public abstract string EmptyMessage { get; }

    protected abstract bool Includes(TaskItem task);

    public virtual async Task LoadAsync()
    {
        ClearAll();

        var all = await Tasks.GetAllAsync();

        foreach (var task in all)
        {
            if (Includes(task))
            {
                AddItem(new TaskItemViewModel(task));
            }
        }

        UpdateEmpty();
        await AfterLoadAsync(all);
    }

    protected virtual void ClearAll()
    {
        foreach (var item in Items)
        {
            Detach(item);
        }

        Items.Clear();
    }

    protected virtual void AddItem(TaskItemViewModel item)
    {
        Attach(item);
        Items.Add(item);
        UpdateEmpty();
    }

    protected virtual void RemoveItem(TaskItemViewModel item)
    {
        Detach(item);
        Items.Remove(item);
        UpdateEmpty();
    }

    protected virtual void UpdateEmpty() => IsEmpty = Items.Count == 0;

    /// <summary>
    /// Runs at the end of every load with the full unfiltered task list; Today
    /// uses it for the Big 3 ritual and the stale-task nudge.
    /// </summary>
    protected virtual Task AfterLoadAsync(IReadOnlyList<TaskItem> allTasks) => Task.CompletedTask;

    protected void Attach(TaskItemViewModel item) => item.CompletionChanged += OnCompletionChanged;

    protected void Detach(TaskItemViewModel item) => item.CompletionChanged -= OnCompletionChanged;

    [RelayCommand]
    private Task StartFocusAsync(TaskItemViewModel item) => Focus.StartAsync(item.Model);

    [RelayCommand]
    private async Task EditAsync(TaskItemViewModel item)
    {
        if (await editor.EditAsync(item.Model))
        {
            item.Refresh();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(TaskItemViewModel item)
    {
        await Tasks.DeleteAsync(item.Model);
        RemoveItem(item);
    }

    private async void OnCompletionChanged(object? sender, EventArgs e)
    {
        if (sender is not TaskItemViewModel item)
        {
            return;
        }

        await Tasks.UpdateAsync(item.Model);

        // A task that no longer belongs on this page leaves it (e.g. ticking a
        // task on Today, or unticking one on Completed).
        if (!Includes(item.Model))
        {
            RemoveItem(item);
        }
    }
}
