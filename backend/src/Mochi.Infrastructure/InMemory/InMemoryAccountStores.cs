using System.Collections.Concurrent;
using Mochi.Application.Abstractions;
using Mochi.Domain.Accounts;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.InMemory;

/// <summary>In-memory user repository for development and tests.</summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email));

    /// <inheritdoc />
    public Task<User?> GetAsync(string id, CancellationToken ct = default)
        => Task.FromResult(_users.GetValueOrDefault(id));

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken ct = default) => Task.FromResult(_users.Count);

    /// <inheritdoc />
    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory session store for development and tests.</summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    /// <inheritdoc />
    public Task AddAsync(Session session, CancellationToken ct = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Session?> GetByTokenHashAsync(byte[] tokenHash, CancellationToken ct = default)
        => Task.FromResult(_sessions.Values.FirstOrDefault(s => s.TokenHash.AsSpan().SequenceEqual(tokenHash)));

    /// <inheritdoc />
    public Task UpdateAsync(Session session, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task RemoveAsync(string sessionId, CancellationToken ct = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAllForUserAsync(string userId, CancellationToken ct = default)
    {
        foreach (var s in _sessions.Values.Where(s => s.UserId == userId)) _sessions.TryRemove(s.Id, out _);
        return Task.CompletedTask;
    }
}

/// <summary>In-memory membership repository for development and tests.</summary>
public sealed class InMemoryMembershipRepository(ISiteRepository sites) : IMembershipRepository
{
    private readonly ConcurrentBag<SiteMembership> _memberships = [];

    /// <inheritdoc />
    public Task<bool> IsMemberAsync(string userId, SiteId siteId, CancellationToken ct = default)
        => Task.FromResult(_memberships.Any(m => m.UserId == userId && m.SiteId == siteId));

    /// <inheritdoc />
    public Task<IReadOnlyList<SiteId>> ListSiteIdsAsync(string userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SiteId>>(
            [.. _memberships.Where(m => m.UserId == userId).Select(m => m.SiteId).Distinct()]);

    /// <inheritdoc />
    public Task AddAsync(SiteMembership membership, CancellationToken ct = default)
    {
        _memberships.Add(membership);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ClaimOrphanedSitesAsync(string userId, CancellationToken ct = default)
    {
        var owned = _memberships.Select(m => m.SiteId).ToHashSet();
        foreach (var site in await sites.ListAsync(ct))
        {
            if (!owned.Contains(site.Id)) _memberships.Add(new SiteMembership(userId, site.Id, SiteRole.Owner));
        }
    }
}
