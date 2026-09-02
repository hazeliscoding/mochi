using System.Collections.Concurrent;
using Mochi.Application.Abstractions;
using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.InMemory;

/// <summary>In-memory event store for development and tests.</summary>
public sealed class InMemoryAnalyticsEventStore : IAnalyticsEventStore
{
    private readonly object _lock = new();
    private readonly List<AnalyticsEvent> _events = [];

    /// <summary>Everything appended so far, oldest first. For tests and debugging.</summary>
    public IReadOnlyCollection<AnalyticsEvent> Events
    {
        get { lock (_lock) return [.. _events]; }
    }

    /// <inheritdoc />
    public Task AppendAsync(AnalyticsEvent evt, CancellationToken ct = default)
    {
        lock (_lock) _events.Add(evt);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<AnalyticsEvent>> ReadDayAsync(SiteId siteId, DateOnly utcDay, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyCollection<AnalyticsEvent>>(
                [.. _events.Where(e => e.SiteId == siteId && DateOnly.FromDateTime(e.OccurredAt.UtcDateTime) == utcDay)]);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<AnalyticsEvent>> ReadRecentAsync(SiteId siteId, DateTimeOffset since, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyCollection<AnalyticsEvent>>(
                [.. _events.Where(e => e.SiteId == siteId && e.OccurredAt >= since)]);
        }
    }

    /// <inheritdoc />
    public Task<long> CountAsync(SiteId siteId, CancellationToken ct = default)
    {
        lock (_lock) return Task.FromResult((long)_events.Count(e => e.SiteId == siteId));
    }

    /// <inheritdoc />
    public Task PurgeBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        lock (_lock) _events.RemoveAll(e => e.OccurredAt < cutoff);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PurgeSiteAsync(SiteId siteId, CancellationToken ct = default)
    {
        lock (_lock) _events.RemoveAll(e => e.SiteId == siteId);
        return Task.CompletedTask;
    }
}
