using Mochi.Application.Rollups;
using Mochi.Domain.Sites;

namespace Mochi.Application.Abstractions;

/// <summary>One day of site stats from the rollups.</summary>
public sealed record DatedSiteStats(DateOnly Date, DailySiteStats Stats);

/// <summary>A rollup row together with the day it belongs to.</summary>
/// <param name="Date">The rolled-up UTC day.</param>
/// <param name="Row">The row.</param>
public sealed record Dated<T>(DateOnly Date, T Row);

/// <summary>
/// Read side of the daily_* rollup tables. Ranges are inclusive UTC days.
/// Today never has rollup rows; the stats service sessionizes today's raw
/// events itself.
/// </summary>
public interface IRollupReader
{
    /// <summary>daily_site_stats rows in range, ordered by date.</summary>
    Task<IReadOnlyList<DatedSiteStats>> SiteStatsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>The site's oldest aggregate day, or null when no rollups exist.</summary>
    Task<DateOnly?> OldestDateAsync(SiteId siteId, CancellationToken ct = default);

    /// <summary>daily_pages rows in range, all days.</summary>
    Task<IReadOnlyList<Dated<DailyPageRow>>> PagesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>daily_sources rows in range, all days.</summary>
    Task<IReadOnlyList<Dated<DailySourceRow>>> SourcesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>daily_geo rows in range, all days.</summary>
    Task<IReadOnlyList<Dated<DailyGeoRow>>> GeoAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>daily_devices rows in range, all days.</summary>
    Task<IReadOnlyList<Dated<DailyDeviceRow>>> DevicesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>daily_events rows in range, all days.</summary>
    Task<IReadOnlyList<Dated<DailyEventRow>>> EventsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
