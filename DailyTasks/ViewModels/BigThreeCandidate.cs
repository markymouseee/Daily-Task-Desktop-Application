using CommunityToolkit.Mvvm.ComponentModel;

namespace DailyTasks.ViewModels;

/// <summary>A row in the "pick your top 3" prompt.</summary>
public partial class BigThreeCandidate(TaskItemViewModel task, Action selectionChanged) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public TaskItemViewModel Task { get; } = task;

    public string Title => Task.Title;

    public string CategoryName => Task.CategoryName;

    public string CategoryColor => Task.CategoryColor;

    partial void OnIsSelectedChanged(bool value) => selectionChanged();
}
