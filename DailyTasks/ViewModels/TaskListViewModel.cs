using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// Shared behaviour for the Today / All Tasks / Completed pages. Shows top-level tasks as
/// recursive cards and hosts their actions. Subclasses decide which roots to show via
/// <see cref="Includes"/>; grouping/sorting live in each page's XAML.
/// </summary>
public abstract partial class TaskListViewModel(ITaskService tasks, FocusService focus, ITaskEditor editor, ITaskCoordinator coordinator)
    : ObservableObject, ITaskCardHost
{
    [ObservableProperty]
    private bool _isEmpty = true;

    protected ITaskService Tasks { get; } = tasks;

    protected FocusService Focus { get; } = focus;

    public ObservableCollection<TaskItemViewModel> Items { get; } = [];

    public abstract string EmptyMessage { get; }

    protected abstract bool Includes(TaskItem task);

    public virtual async Task LoadAsync()
    {
        ClearAll();

        var roots = await Tasks.GetRootsAsync();

        foreach (var root in roots)
        {
            if (Includes(root))
            {
                AddItem(new TaskItemViewModel(root, this));
            }
        }

        UpdateEmpty();
        await AfterLoadAsync(roots);
    }

    protected virtual void ClearAll() => Items.Clear();

    protected virtual void AddItem(TaskItemViewModel item)
    {
        Items.Add(item);
        UpdateEmpty();
    }

    protected virtual void RemoveItem(TaskItemViewModel item)
    {
        Items.Remove(item);
        UpdateEmpty();
    }

    protected virtual void UpdateEmpty() => IsEmpty = Items.Count == 0;

    /// <summary>Runs at the end of every load with the top-level roots.</summary>
    protected virtual Task AfterLoadAsync(IReadOnlyList<TaskItem> roots) => Task.CompletedTask;

    /// <summary>New-task dialog, then refresh the list.</summary>
    [RelayCommand]
    private async Task NewTask()
    {
        if (await coordinator.CreateTaskAsync())
        {
            await LoadAsync();
        }
    }

    // ---- ITaskCardHost ----

    public Task StartFocusAsync(TaskItemViewModel node) => Focus.StartAsync(node.Model);

    public async Task EditAsync(TaskItemViewModel node)
    {
        if (await editor.EditAsync(node.Model))
        {
            node.Refresh();
            RefreshAncestors(node);
        }
    }

    public async Task DeleteAsync(TaskItemViewModel node)
    {
        await Tasks.DeleteAsync(node.Model);
        RemoveNode(node);
    }

    public async Task AddChildAsync(TaskItemViewModel node, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var child = new TaskItem
        {
            Title = title.Trim(),
            CategoryId = node.Model.CategoryId,
            Category = node.Model.Category,
            ParentTaskId = node.Model.Id,
            Priority = TaskPriority.Medium,
        };

        await Tasks.AddAsync(child);

        node.Model.Children.Add(child);
        node.Children.Add(new TaskItemViewModel(child, this, node.Depth + 1) { ParentNode = node });
        node.IsExpanded = true;

        node.RefreshRollup();
        RefreshAncestors(node);
    }

    public virtual async Task OrganizeAsync(TaskItemViewModel node)
    {
        if (await coordinator.OrganizeAsync(node.Model))
        {
            await LoadAsync();
        }
    }

    public virtual async Task OpenDetailAsync(TaskItemViewModel node)
    {
        await coordinator.OpenDetailAsync(node.Model.Id);
        await LoadAsync();
    }

    public async Task ChangedAsync(TaskItemViewModel node)
    {
        // Recurrence only spawns for top-level tasks; children are a plain update.
        if (node.Model.IsCompleted && node.ParentNode is null)
        {
            await Tasks.CompleteAsync(node.Model);
        }
        else
        {
            await Tasks.UpdateAsync(node.Model);
        }

        RefreshAncestors(node);

        // A root that no longer belongs on this page leaves it.
        if (node.ParentNode is null && !Includes(node.Model))
        {
            RemoveItem(node);
        }
    }

    private void RemoveNode(TaskItemViewModel node)
    {
        if (node.ParentNode is { } parent)
        {
            parent.Children.Remove(node);
            parent.Model.Children.Remove(node.Model);
            parent.RefreshRollup();
            RefreshAncestors(parent);
        }
        else
        {
            RemoveItem(node);
        }
    }

    protected static void RefreshAncestors(TaskItemViewModel node)
    {
        var ancestor = node.ParentNode;
        while (ancestor is not null)
        {
            ancestor.RefreshRollup();
            ancestor = ancestor.ParentNode;
        }
    }
}
