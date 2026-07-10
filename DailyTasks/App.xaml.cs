using System.IO;
using System.Windows;
using System.Windows.Interop;
using DailyTasks.Data;
using DailyTasks.Services;
using DailyTasks.ViewModels;
using DailyTasks.Views;
using DailyTasks.Views.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;

namespace DailyTasks;

public partial class App : Application
{
    private ServiceProvider? _services;
    private QuickCaptureWindow? _quickCapture;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(Path.GetDirectoryName(AppDbContext.DatabasePath)!);

        _services = ConfigureServices();

        using (var db = _services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext())
        {
            db.Database.Migrate();
        }

        var settings = _services.GetRequiredService<SettingsService>();
        settings.Load();
        ApplicationThemeManager.Apply(settings.Theme);

        var main = _services.GetRequiredService<MainWindow>();
        main.Show();

        // The handle only exists once the window is shown.
        var hotkeys = _services.GetRequiredService<GlobalHotkeyService>();
        hotkeys.Pressed += (_, _) => ShowQuickCapture();
        hotkeys.Register(new WindowInteropHelper(main).Handle);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.GetService<GlobalHotkeyService>()?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }

    private void ShowQuickCapture()
    {
        if (_quickCapture is not null)
        {
            _quickCapture.Activate();
            return;
        }

        _quickCapture = _services!.GetRequiredService<QuickCaptureWindow>();
        _quickCapture.Closed += (_, _) => _quickCapture = null;
        _quickCapture.Show();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(AppDbContext.ConnectionString));

        services.AddSingleton<ITaskService, TaskService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<GlobalHotkeyService>();
        services.AddSingleton<INavigationViewPageProvider, PageProvider>();

        services.AddSingleton<MainWindow>();

        // Today is a singleton so a quick-capture save shows up on an already-open page.
        services.AddSingleton<TodayViewModel>();

        services.AddTransient<TodayPage>();
        services.AddTransient<AllTasksPage>();
        services.AddTransient<AllTasksViewModel>();
        services.AddTransient<CompletedPage>();
        services.AddTransient<CompletedViewModel>();
        services.AddTransient<InsightsPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<QuickCaptureWindow>();
        services.AddTransient<QuickCaptureViewModel>();

        return services.BuildServiceProvider();
    }
}
