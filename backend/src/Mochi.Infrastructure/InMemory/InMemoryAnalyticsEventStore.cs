using System.Collections.Concurrent;
using Mochi.Application.Abstractions;
using Mochi.Domain.Collection;

namespace Mochi.Infrastructure.InMemory;

/// <summary>In-memory event store for development. Replaced by EF Core and Postgres in v0.2.</summary>
public sealed class InMemoryAnalyticsEventStore : IAnalyticsEventStore
{
    private readonly ConcurrentQueue<AnalyticsEvent> _events = new();

    /// <summary>Everything appended so far, oldest first. For tests and debugging.</summary>
    public IReadOnlyCollection<AnalyticsEvent> Events => _events;

    /// <inheritdoc />
    public Task AppendAsync(AnalyticsEvent evt, CancellationToken ct = default)
    {
        _events.Enqueue(evt);
        return Task.CompletedTask;
    }
}
