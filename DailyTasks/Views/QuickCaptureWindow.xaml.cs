using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

public partial class QuickCaptureWindow : Window
{
    private bool _hasBeenActivated;
    private bool _isClosing;

    public QuickCaptureWindow(QuickCaptureViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.Saved += (_, _) => CloseOnce();

        Loaded += OnLoaded;
        Activated += (_, _) => _hasBeenActivated = true;
        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _isClosing = true;
        base.OnClosing(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearTopOfScreen();

        // The hotkey grants us foreground rights, so this actually takes focus.
        Activate();
        CaptureBox.Focus();
    }

    private void PositionNearTopOfScreen()
    {
        var area = SystemParameters.WorkArea;

        Left = area.Left + ((area.Width - ActualWidth) / 2);
        Top = area.Top + (area.Height * 0.18);
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Ignore the spurious deactivate that arrives before we ever had focus.
        if (_hasBeenActivated)
        {
            CloseOnce();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape)
        {
            e.Handled = true;
            CloseOnce();
        }
    }

    /// <summary>
    /// Closing the window deactivates it, which re-enters OnDeactivated. Calling
    /// Close() a second time throws inside the WndProc and kills the process.
    /// </summary>
    private void CloseOnce()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        Close();
    }
}
