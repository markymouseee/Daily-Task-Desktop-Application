using System.Windows.Threading;
using DailyTasks.Models;

namespace DailyTasks.Services;

public enum FocusState
{
    Idle,
    Running,
    Paused,
}

public enum FocusEndKind
{
    /// <summary>User stopped early — the task was left incomplete.</summary>
    Stopped,

    /// <summary>The timer ran its full course.</summary>
    Completed,

    /// <summary>A new session started on another task; ended without ceremony.</summary>
    Replaced,
}

public sealed class FocusSessionEndedEventArgs(TaskItem task, TimeSpan elapsed, FocusEndKind kind) : EventArgs
{
    public TaskItem Task { get; } = task;

    public TimeSpan Elapsed { get; } = elapsed;

    public FocusEndKind Kind { get; } = kind;
}

/// <summary>
/// The one running Pomodoro. Owns the countdown, the pause bookkeeping, and the
/// interruption log; accrues elapsed minutes onto the task's ActualMinutes when
/// the session ends however it ends.
/// </summary>
public sealed class FocusService
{
    private readonly ITaskService _tasks;
    private readonly IInterruptionService _interruptions;
    private readonly SettingsService _settings;
    private readonly DispatcherTimer _timer;

    private DateTime _runStart;
    private TimeSpan _accumulated;
    private DateTime _pauseStart;
    private InterruptionEvent? _openInterruption;
    private bool _ending;

    public FocusService(ITaskService tasks, IInterruptionService interruptions, SettingsService settings)
    {
        _tasks = tasks;
        _interruptions = interruptions;
        _settings = settings;

        _timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTick;
    }

    /// <summary>Fires every second while active and on every state change.</summary>
    public event EventHandler? Changed;

    public event EventHandler<FocusSessionEndedEventArgs>? SessionEnded;

    public FocusState State { get; private set; }

    public TaskItem? Task { get; private set; }

    public TimeSpan Duration { get; private set; }

    public TimeSpan Elapsed =>
        State == FocusState.Running ? _accumulated + (DateTime.Now - _runStart) : _accumulated;

    public TimeSpan Remaining
    {
        get
        {
            var left = Duration - Elapsed;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }

    public async Task StartAsync(TaskItem task)
    {
        if (State != FocusState.Idle)
        {
            await EndAsync(FocusEndKind.Replaced);
        }

        Task = task;
        Duration = TimeSpan.FromMinutes(Math.Max(1, _settings.PomodoroMinutes));
        _accumulated = TimeSpan.Zero;
        _runStart = DateTime.Now;
        State = FocusState.Running;

        _timer.Start();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task InterruptAsync()
    {
        if (State != FocusState.Running)
        {
            return;
        }

        _accumulated += DateTime.Now - _runStart;
        _pauseStart = DateTime.Now;
        State = FocusState.Paused;

        // Persist immediately so the timestamp survives even if the app dies mid-pause.
        _openInterruption = new InterruptionEvent
        {
            TaskItemId = Task!.Id,
            OccurredAt = _pauseStart,
        };
        await _interruptions.LogAsync(_openInterruption);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ResumeAsync(InterruptionReason? reason)
    {
        if (State != FocusState.Paused)
        {
            return;
        }

        await CloseOpenInterruptionAsync(reason);

        _runStart = DateTime.Now;
        State = FocusState.Running;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task StopAsync() => EndAsync(FocusEndKind.Stopped);

    private async void OnTick(object? sender, EventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);

        if (State == FocusState.Running && Elapsed >= Duration)
        {
            await EndAsync(FocusEndKind.Completed);
        }
    }

    private async Task EndAsync(FocusEndKind kind)
    {
        if (State == FocusState.Idle || _ending)
        {
            return;
        }

        // OnTick keeps firing while the awaits below run; don't end twice.
        _ending = true;
        _timer.Stop();

        if (State == FocusState.Running)
        {
            _accumulated += DateTime.Now - _runStart;
        }

        await CloseOpenInterruptionAsync(null);

        var task = Task!;
        var elapsed = _accumulated;

        var minutes = (int)Math.Round(elapsed.TotalMinutes);
        if (minutes > 0)
        {
            task.ActualHours = (task.ActualHours ?? 0) + (minutes / 60.0);
            await _tasks.UpdateAsync(task);
        }

        Task = null;
        State = FocusState.Idle;
        _accumulated = TimeSpan.Zero;
        _ending = false;

        Changed?.Invoke(this, EventArgs.Empty);
        SessionEnded?.Invoke(this, new FocusSessionEndedEventArgs(task, elapsed, kind));
    }

    private async Task CloseOpenInterruptionAsync(InterruptionReason? reason)
    {
        if (_openInterruption is null)
        {
            return;
        }

        _openInterruption.Reason = reason ?? InterruptionReason.Other;
        _openInterruption.MinutesLost = Math.Max(0, (int)Math.Round((DateTime.Now - _pauseStart).TotalMinutes));
        await _interruptions.UpdateAsync(_openInterruption);
        _openInterruption = null;
    }
}
