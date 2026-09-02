using Microsoft.EntityFrameworkCore;
using Mochi.Application.Abstractions;
using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Postgres-backed raw event store.</summary>
public sealed class EfAnalyticsEventStore(MochiDbContext db) : IAnalyticsEventStore
{
    /// <inheritdoc />
    public async Task AppendAsync(AnalyticsEvent evt, CancellationToken ct = default)
    {
        db.Events.Add(evt);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AnalyticsEvent>> ReadDayAsync(SiteId siteId, DateOnly utcDay, CancellationToken ct = default)
    {
        var start = new DateTimeOffset(utcDay, TimeOnly.MinValue, TimeSpan.Zero);
        var end = start.AddDays(1);
        return await db.Events
            .Where(e => e.SiteId == siteId && e.OccurredAt >= start && e.OccurredAt < end)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AnalyticsEvent>> ReadRecentAsync(SiteId siteId, DateTimeOffset since, CancellationToken ct = default)
        => await db.Events.Where(e => e.SiteId == siteId && e.OccurredAt >= since).ToListAsync(ct);

    /// <inheritdoc />
    public Task PurgeBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default)
        => db.Events.Where(e => e.OccurredAt < cutoff).ExecuteDeleteAsync(ct);

    /// <inheritdoc />
    public Task PurgeSiteAsync(SiteId siteId, CancellationToken ct = default)
        => db.Events.Where(e => e.SiteId == siteId).ExecuteDeleteAsync(ct);
}
