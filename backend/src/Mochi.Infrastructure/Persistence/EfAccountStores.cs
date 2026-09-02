using Microsoft.EntityFrameworkCore;
using Mochi.Application.Abstractions;
using Mochi.Domain.Accounts;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Postgres-backed user repository.</summary>
public sealed class EfUserRepository(MochiDbContext db) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    /// <inheritdoc />
    public Task<User?> GetAsync(string id, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken ct = default) => db.Users.CountAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Postgres-backed session store.</summary>
public sealed class EfSessionStore(MochiDbContext db) : ISessionStore
{
    /// <inheritdoc />
    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public Task<Session?> GetByTokenHashAsync(byte[] tokenHash, CancellationToken ct = default)
        => db.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, ct);

    /// <inheritdoc />
    public Task UpdateAsync(Session session, CancellationToken ct = default)
        => db.SaveChangesAsync(ct);

    /// <inheritdoc />
    public Task RemoveAsync(string sessionId, CancellationToken ct = default)
        => db.Sessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(ct);

    /// <inheritdoc />
    public Task RemoveAllForUserAsync(string userId, CancellationToken ct = default)
        => db.Sessions.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
}

/// <summary>Postgres-backed membership repository.</summary>
public sealed class EfMembershipRepository(MochiDbContext db) : IMembershipRepository
{
    /// <inheritdoc />
    public Task<bool> IsMemberAsync(string userId, SiteId siteId, CancellationToken ct = default)
        => db.Memberships.AnyAsync(m => m.UserId == userId && m.SiteId == siteId, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SiteId>> ListSiteIdsAsync(string userId, CancellationToken ct = default)
        => await db.Memberships.Where(m => m.UserId == userId).Select(m => m.SiteId).ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(SiteMembership membership, CancellationToken ct = default)
    {
        db.Memberships.Add(membership);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task ClaimOrphanedSitesAsync(string userId, CancellationToken ct = default)
    {
        var orphans = await db.Sites
            .Where(s => !db.Memberships.Any(m => m.SiteId == s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);
        db.Memberships.AddRange(orphans.Select(id => new SiteMembership(userId, id, SiteRole.Owner)));
        await db.SaveChangesAsync(ct);
    }
}
