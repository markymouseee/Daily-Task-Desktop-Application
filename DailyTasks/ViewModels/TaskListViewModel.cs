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
public abstract partial class TaskListViewModel(ITaskService tasks) : ObservableObject
{
    [ObservableProperty]
    private bool _isEmpty = true;

    protected ITaskService Tasks { get; } = tasks;

    public ObservableCollection<TaskItemViewModel> Items { get; } = [];

    /// <summary>Shown when the page has no tasks to display.</summary>
    public abstract string EmptyMessage { get; }

    protected abstract bool Includes(TaskItem task);

    public virtual async Task LoadAsync()
    {
        ClearAll();

        foreach (var task in await Tasks.GetAllAsync())
        {
            if (Includes(task))
            {
                AddItem(new TaskItemViewModel(task));
            }
        }

        UpdateEmpty();
        AfterLoad();
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

    /// <summary>Runs at the end of every load; Today uses it to raise its ritual prompt.</summary>
    protected virtual void AfterLoad()
    {
    }

    protected void Attach(TaskItemViewModel item) => item.CompletionChanged += OnCompletionChanged;

    protected void Detach(TaskItemViewModel item) => item.CompletionChanged -= OnCompletionChanged;

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
