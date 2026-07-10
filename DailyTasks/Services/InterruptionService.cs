using DailyTasks.Data;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Services;

public interface IInterruptionService
{
    Task LogAsync(InterruptionEvent interruption);

    Task UpdateAsync(InterruptionEvent interruption);

    Task<IReadOnlyList<InterruptionEvent>> GetSinceAsync(DateTime since);
}

public sealed class InterruptionService(IDbContextFactory<AppDbContext> factory) : IInterruptionService
{
    public async Task LogAsync(InterruptionEvent interruption)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Interruptions.Add(interruption);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(InterruptionEvent interruption)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Interruptions.Update(interruption);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<InterruptionEvent>> GetSinceAsync(DateTime since)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interruptions.AsNoTracking()
            .Where(i => i.OccurredAt >= since)
            .ToListAsync();
    }
}
