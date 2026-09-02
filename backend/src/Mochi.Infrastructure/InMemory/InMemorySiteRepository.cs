using System.Collections.Concurrent;
using Mochi.Application.Abstractions;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.InMemory;

/// <summary>In-memory site store for development. Replaced by EF Core and Postgres in v0.2.</summary>
public sealed class InMemorySiteRepository : ISiteRepository
{
    private readonly ConcurrentDictionary<SiteId, Site> _sites = new();

    /// <inheritdoc />
    public Task<Site?> GetAsync(SiteId id, CancellationToken ct = default)
        => Task.FromResult(_sites.GetValueOrDefault(id));

    /// <inheritdoc />
    public Task<IReadOnlyList<Site>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Site>>([.. _sites.Values.OrderByDescending(s => s.CreatedAt)]);

    /// <inheritdoc />
    public Task AddAsync(Site site, CancellationToken ct = default)
    {
        _sites[site.Id] = site;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Site site, CancellationToken ct = default)
    {
        _sites[site.Id] = site;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(SiteId id, CancellationToken ct = default)
    {
        _sites.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
