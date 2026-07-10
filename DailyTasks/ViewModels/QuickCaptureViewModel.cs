using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public partial class QuickCaptureViewModel(ITaskService tasks, ICategoryService categories) : ObservableObject
{
    private string _parsedTitle = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _preview = string.Empty;

    /// <summary>Raised once the task is stored, so the window can dismiss itself.</summary>
    public event EventHandler? Saved;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var all = await categories.GetAllAsync();

        if (all.Count == 0)
        {
            return;
        }

        var parsed = TaskTextParser.Parse(Text);
        var category = all[0];

        await tasks.AddAsync(new TaskItem
        {
            Title = parsed.Title,
            CategoryId = category.Id,
            Category = category,
            Priority = parsed.Priority ?? TaskPriority.Medium,

            // Undated captures land on Today rather than disappearing into All Tasks.
            DueDate = parsed.DueDate ?? DateTime.Today,
        });

        Text = string.Empty;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave() => _parsedTitle.Length > 0;

    partial void OnTextChanged(string value)
    {
        var parsed = TaskTextParser.Parse(value);

        _parsedTitle = parsed.Title;
        Preview = parsed.HasHints ? $"{parsed.Title} — {parsed.Summary}" : string.Empty;

        SaveCommand.NotifyCanExecuteChanged();
    }
}
