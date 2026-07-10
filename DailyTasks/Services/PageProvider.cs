using Wpf.Ui.Abstractions;

namespace DailyTasks.Services;

/// <summary>
/// Lets WPF-UI's NavigationView resolve pages from the DI container.
/// </summary>
public sealed class PageProvider(IServiceProvider services) : INavigationViewPageProvider
{
    public object? GetPage(Type pageType) => services.GetService(pageType);
}
