using Microsoft.EntityFrameworkCore;
using Mochi.Application.Abstractions;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Postgres-backed site repository.</summary>
public sealed class EfSiteRepository(MochiDbContext db) : ISiteRepository
{
    /// <inheritdoc />
    public Task<Site?> GetAsync(SiteId id, CancellationToken ct = default)
        => db.Sites.FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Site>> ListAsync(CancellationToken ct = default)
        => await db.Sites.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(Site site, CancellationToken ct = default)
    {
        db.Sites.Add(site);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Site site, CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    /// <inheritdoc />
    public Task RemoveAsync(SiteId id, CancellationToken ct = default)
        => db.Sites.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
}
