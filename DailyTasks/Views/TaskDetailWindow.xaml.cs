using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DailyTasks.Models;
using DailyTasks.ViewModels;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class TaskDetailWindow : FluentWindow
{
    private TaskItemViewModel? _dragCandidate;
    private Point _dragStart;

    public TaskDetailWindow(TaskDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private TaskDetailViewModel ViewModel => (TaskDetailViewModel)DataContext;

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TaskDetailViewModel.HighlightedTaskId))
        {
            ScrollHighlightedIntoView();
        }
    }

    private void ScrollHighlightedIntoView()
    {
        if (ViewModel.HighlightedTaskId is not { } id)
        {
            return;
        }

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
        {
            var target = Descendants(this)
                .OfType<FrameworkElement>()
                .FirstOrDefault(fe => fe.DataContext is TaskItemViewModel t && t.Model.Id == id);

            target?.BringIntoView();
        }));
    }

    // ---- Kanban drag and drop ----

    private void OnCardPreviewDown(object sender, MouseButtonEventArgs e)
    {
        _dragCandidate = FindTask(e.OriginalSource as DependencyObject);
        _dragStart = e.GetPosition(null);
    }

    private void OnCardPreviewMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate is null)
        {
            return;
        }

        var moved = _dragStart - e.GetPosition(null);
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var card = _dragCandidate;
        _dragCandidate = null;
        DragDrop.DoDragDrop((DependencyObject)sender, card, DragDropEffects.Move);
    }

    private void OnColumnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(TaskItemViewModel)) is TaskItemViewModel card
            && sender is FrameworkElement { Tag: WorkStatus status })
        {
            ViewModel.Move(card, status);
        }
    }

    private static TaskItemViewModel? FindTask(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: TaskItemViewModel vm })
            {
                return vm;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private static System.Collections.Generic.IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var d in Descendants(child))
            {
                yield return d;
            }
        }
    }
}
