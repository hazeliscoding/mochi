using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Application.Abstractions;

/// <summary>
/// Store for raw scrubbed events. Rows live at most 7 days; the rollup job
/// reads closed days and purges (ADR 0003).
/// </summary>
public interface IAnalyticsEventStore
{
    /// <summary>Appends one event.</summary>
    Task AppendAsync(AnalyticsEvent evt, CancellationToken ct = default);

    /// <summary>All events for one site on one UTC day, any order.</summary>
    Task<IReadOnlyCollection<AnalyticsEvent>> ReadDayAsync(SiteId siteId, DateOnly utcDay, CancellationToken ct = default);

    /// <summary>All events for one site since <paramref name="since"/>. Serves the realtime view.</summary>
    Task<IReadOnlyCollection<AnalyticsEvent>> ReadRecentAsync(SiteId siteId, DateTimeOffset since, CancellationToken ct = default);

    /// <summary>Raw events currently held for one site. Serves the privacy summary.</summary>
    Task<long> CountAsync(SiteId siteId, CancellationToken ct = default);

    /// <summary>Deletes events older than <paramref name="cutoff"/> across all sites.</summary>
    Task PurgeBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default);

    /// <summary>Deletes all events for one site. Used by site deletion.</summary>
    Task PurgeSiteAsync(SiteId siteId, CancellationToken ct = default);
}
