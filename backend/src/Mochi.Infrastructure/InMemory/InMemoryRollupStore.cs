using System.Collections.Concurrent;
using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.InMemory;

/// <summary>In-memory rollup store for development and tests.</summary>
public sealed class InMemoryRollupStore : IRollupStore
{
    private readonly ConcurrentDictionary<(SiteId, DateOnly), RollupBatch> _days = new();

    /// <summary>All stored batches. For tests and debugging.</summary>
    public IReadOnlyCollection<RollupBatch> Batches => [.. _days.Values];

    /// <inheritdoc />
    public Task ReplaceDayAsync(RollupBatch batch, CancellationToken ct = default)
    {
        _days[(batch.SiteId, batch.Date)] = batch;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PurgeBeforeAsync(SiteId siteId, DateOnly cutoff, CancellationToken ct = default)
    {
        foreach (var key in _days.Keys.Where(k => k.Item1 == siteId && k.Item2 < cutoff))
        {
            _days.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PurgeSiteAsync(SiteId siteId, CancellationToken ct = default)
    {
        foreach (var key in _days.Keys.Where(k => k.Item1 == siteId))
        {
            _days.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
