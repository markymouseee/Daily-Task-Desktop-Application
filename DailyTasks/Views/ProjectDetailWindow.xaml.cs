using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DailyTasks.Models;
using DailyTasks.ViewModels;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class ProjectDetailWindow : FluentWindow
{
    private SubtaskViewModel? _dragCandidate;
    private Point _dragStart;

    public ProjectDetailWindow(ProjectDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private ProjectDetailViewModel ViewModel => (ProjectDetailViewModel)DataContext;

    // ---- Kanban drag and drop ----

    private void OnCardPreviewDown(object sender, MouseButtonEventArgs e)
    {
        _dragCandidate = FindSubtask(e.OriginalSource as DependencyObject);
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
        if (e.Data.GetData(typeof(SubtaskViewModel)) is SubtaskViewModel card
            && sender is FrameworkElement { Tag: SubtaskStatus status })
        {
            ViewModel.Move(card, status);
        }
    }

    /// <summary>Walks up from the clicked element to the card bound to a subtask.</summary>
    private static SubtaskViewModel? FindSubtask(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: SubtaskViewModel vm })
            {
                return vm;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }
}
