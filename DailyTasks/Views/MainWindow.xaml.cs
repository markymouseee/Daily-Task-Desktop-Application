using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DailyTasks.Services;
using DailyTasks.ViewModels;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class MainWindow : FluentWindow
{
    private readonly INavigationViewPageProvider _pageProvider;
    private readonly SettingsService _settings;

    private object? _currentPage;

    public MainWindow(INavigationViewPageProvider pageProvider, FocusViewModel focusViewModel, SettingsService settings)
    {
        _pageProvider = pageProvider;
        _settings = settings;

        InitializeComponent();

        SessionBar.DataContext = focusViewModel;

        UpdateIdentity();
        UpdateThemeIcon();

        // Selecting Today raises Checked, which performs the first navigation.
        Loaded += (_, _) =>
        {
            TodayNav.IsChecked = true;

            // First run: show the walkthrough once the Today page has painted behind it.
            if (!_settings.TutorialCompleted)
            {
                Dispatcher.BeginInvoke(ShowWelcomeTour, DispatcherPriority.Background);
            }
        };
    }

    private void ShowWelcomeTour()
    {
        new WelcomeWindow { Owner = this }.ShowDialog();
        _settings.TutorialCompleted = true;
    }

    private async void OnNavChecked(object sender, RoutedEventArgs e)
    {
        // Ignore the checks that fire while the template is still being applied.
        if (!IsLoaded || sender is not RadioButton { Tag: Type pageType })
        {
            return;
        }

        await NavigateAsync(pageType);
    }

    /// <summary>
    /// Resolves the page from DI and swaps it into the frame, preserving the
    /// INavigationAware load/unload hooks the pages rely on for their data.
    /// </summary>
    private async Task NavigateAsync(Type pageType)
    {
        if (_currentPage is INavigationAware leaving)
        {
            await leaving.OnNavigatedFromAsync();
        }

        var page = _pageProvider.GetPage(pageType);
        _currentPage = page;
        ContentFrame.Navigate(page);

        if (page is INavigationAware entering)
        {
            await entering.OnNavigatedToAsync();
        }

        // The name may have been edited on the Settings page we just left.
        UpdateIdentity();
    }

    private void UpdateIdentity()
    {
        var name = _settings.DisplayName;
        UserNameText.Text = name;
        AvatarInitials.Text = Initials(name);
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        var next = _settings.Theme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(next);
        _settings.Theme = next; // persists
        UpdateThemeIcon();
    }

    private void UpdateThemeIcon() =>
        ThemeToggle.Icon = new SymbolIcon
        {
            Symbol = _settings.Theme == ApplicationTheme.Dark ? SymbolRegular.WeatherMoon24 : SymbolRegular.WeatherSunny24,
        };

    private static string Initials(string name)
    {
        var parts = name.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);

        var initials = parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1],
            _ => $"{parts[0][0]}{parts[^1][0]}",
        };

        return initials.ToUpperInvariant();
    }
}
