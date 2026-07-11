using DailyTasks.Data;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Services;

public interface ITeamService
{
    Task<IReadOnlyList<TeamMember>> GetAllAsync();

    Task AddAsync(TeamMember member);

    Task UpdateAsync(TeamMember member);

    Task DeleteAsync(TeamMember member);
}

/// <summary>Short-lived context per call, matching the other services.</summary>
public sealed class TeamService(IDbContextFactory<AppDbContext> factory) : ITeamService
{
    public async Task<IReadOnlyList<TeamMember>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.TeamMembers.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
    }

    public async Task AddAsync(TeamMember member)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TeamMember member)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.TeamMembers.FindAsync(member.Id);
        if (existing is null)
        {
            return;
        }

        db.Entry(existing).CurrentValues.SetValues(member);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(TeamMember member)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.TeamMembers.FindAsync(member.Id);
        if (existing is not null)
        {
            // The FK is SetNull, so assigned subtasks become unassigned rather than deleted.
            db.TeamMembers.Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}
