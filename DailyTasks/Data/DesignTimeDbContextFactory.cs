using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DailyTasks.Data;

/// <summary>
/// Only used by "dotnet ef" at design time. The running app builds its context
/// through DI in App.xaml.cs instead.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(AppDbContext.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
