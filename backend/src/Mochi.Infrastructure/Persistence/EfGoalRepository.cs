using Microsoft.EntityFrameworkCore;
using Mochi.Application.Abstractions;
using Mochi.Domain.Goals;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Postgres-backed goal repository.</summary>
public sealed class EfGoalRepository(MochiDbContext db) : IGoalRepository
{
    /// <inheritdoc />
    public Task<Goal?> GetAsync(SiteId siteId, string goalId, CancellationToken ct = default)
        => db.Goals.FirstOrDefaultAsync(g => g.SiteId == siteId && g.Id == goalId, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Goal>> ListAsync(SiteId siteId, CancellationToken ct = default)
        => await db.Goals.Where(g => g.SiteId == siteId).OrderByDescending(g => g.CreatedAt).ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(Goal goal, CancellationToken ct = default)
    {
        db.Goals.Add(goal);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task RemoveAsync(SiteId siteId, string goalId, CancellationToken ct = default)
        => db.Goals.Where(g => g.SiteId == siteId && g.Id == goalId).ExecuteDeleteAsync(ct);
}
