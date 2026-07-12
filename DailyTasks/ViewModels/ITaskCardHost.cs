namespace DailyTasks.ViewModels;

/// <summary>
/// The actions a task card delegates to its owning list. Passed down the recursive tree so
/// every node — at any depth — routes edit/delete/add-child/organize back to one owner.
/// </summary>
public interface ITaskCardHost
{
    Task EditAsync(TaskItemViewModel node);

    Task DeleteAsync(TaskItemViewModel node);

    Task StartFocusAsync(TaskItemViewModel node);

    /// <summary>Add a child under this node (the inline "add a subtask" affordance).</summary>
    Task AddChildAsync(TaskItemViewModel node, string title);

    /// <summary>Apply/patch a methodology on a node that has children.</summary>
    Task OrganizeAsync(TaskItemViewModel node);

    /// <summary>Open the methodology detail (phases / Kanban / Gantt / export).</summary>
    Task OpenDetailAsync(TaskItemViewModel node);

    /// <summary>Persist a status/completion change and refresh rollups.</summary>
    Task ChangedAsync(TaskItemViewModel node);
}
