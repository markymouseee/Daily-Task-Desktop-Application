using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>Drives the focus bar docked under the title bar.</summary>
public partial class FocusViewModel : ObservableObject
{
    private readonly FocusService _focus;
    private readonly ITaskEditor _editor;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _taskTitle = string.Empty;

    [ObservableProperty]
    private string _remainingText = string.Empty;

    public FocusViewModel(FocusService focus, ITaskEditor editor)
    {
        _focus = focus;
        _editor = editor;

        focus.Changed += (_, _) => Refresh();
        focus.SessionEnded += OnSessionEnded;
    }

    public IReadOnlyList<InterruptionReason> Reasons { get; } =
        [InterruptionReason.Meeting, InterruptionReason.Message, InterruptionReason.Person, InterruptionReason.Other];

    private void Refresh()
    {
        IsActive = _focus.State != FocusState.Idle;
        IsPaused = _focus.State == FocusState.Paused;
        TaskTitle = _focus.Task?.Title ?? string.Empty;
        RemainingText = _focus.Remaining.ToString(@"mm\:ss");
    }

    [RelayCommand]
    private Task InterruptAsync() => _focus.InterruptAsync();

    [RelayCommand]
    private Task ResumeWithReasonAsync(InterruptionReason reason) => _focus.ResumeAsync(reason);

    [RelayCommand]
    private Task ResumeAsync() => _focus.ResumeAsync(null);

    [RelayCommand]
    private Task StopAsync() => _focus.StopAsync();

    private async void OnSessionEnded(object? sender, FocusSessionEndedEventArgs e)
    {
        // Leaving a task mid-flight is the moment to write down where you were.
        if (e.Kind == FocusEndKind.Stopped && !e.Task.IsCompleted)
        {
            await _editor.PromptResumeNoteAsync(e.Task);
        }
    }
}
