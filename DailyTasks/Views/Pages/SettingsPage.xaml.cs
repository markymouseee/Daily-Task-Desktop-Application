using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DailyTasks.ViewModels;

namespace DailyTasks.Views.Pages;

public partial class SettingsPage : Page
{
    // Where beta testers' feedback goes. Change this to your own address before sharing.
    private const string FeedbackEmail = "sanamamarknoriel9@gmail.com";

    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnReplayTour(object sender, RoutedEventArgs e) =>
        new WelcomeWindow { Owner = Window.GetWindow(this) }.ShowDialog();

    private void OnSendFeedback(object sender, RoutedEventArgs e)
    {
        var subject = Uri.EscapeDataString("DailyTasks beta feedback");
        var body = Uri.EscapeDataString(
            $"\n\n\n— — —\nApp: DailyTasks 0.9.0-beta\nWindows: {Environment.OSVersion.Version}\n");

        try
        {
            Process.Start(new ProcessStartInfo($"mailto:{FeedbackEmail}?subject={subject}&body={body}")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // No mail client configured — nothing we can do, so fail quietly.
        }
    }
}
