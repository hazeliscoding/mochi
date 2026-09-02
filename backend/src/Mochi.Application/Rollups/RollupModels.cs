using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Application.Rollups;

/// <summary>One row of daily_site_stats (ADR 0003).</summary>
/// <param name="Visitors">Distinct visitor hashes that day.</param>
/// <param name="Pageviews">Pageview count.</param>
/// <param name="Sessions">Session count.</param>
/// <param name="BouncedSessions">Sessions with a single pageview.</param>
/// <param name="TotalSessionDurationSec">Sum of session durations. Divide by sessions for the average.</param>
public sealed record DailySiteStats(int Visitors, int Pageviews, int Sessions, int BouncedSessions, long TotalSessionDurationSec);

/// <summary>One row of daily_pages per path.</summary>
public sealed record DailyPageRow(string Path, int Visitors, int Pageviews, int Entries, int Exits, int BouncedSessions, long TotalDurationSec);

/// <summary>One row of daily_sources per channel, referrer domain and campaign.</summary>
public sealed record DailySourceRow(Channel Channel, string? ReferrerDomain, string? Campaign, int Visitors, int Pageviews, int BouncedSessions);

/// <summary>One row of daily_geo per country.</summary>
public sealed record DailyGeoRow(string Country, int Visitors);

/// <summary>One row of daily_devices per device class, browser and OS.</summary>
public sealed record DailyDeviceRow(DeviceClass Device, string Browser, string Os, int Visitors);

/// <summary>One row of daily_events per event name, path and channel.</summary>
public sealed record DailyEventRow(string EventName, string Path, Channel Channel, int Total, int UniqueVisitors);

/// <summary>Everything the rollup job writes for one site and one day.</summary>
/// <param name="SiteId">Site the batch belongs to.</param>
/// <param name="Date">The closed UTC day.</param>
/// <param name="SiteStats">The daily_site_stats row.</param>
/// <param name="Pages">daily_pages rows.</param>
/// <param name="Sources">daily_sources rows.</param>
/// <param name="Geo">daily_geo rows.</param>
/// <param name="Devices">daily_devices rows.</param>
/// <param name="Events">daily_events rows.</param>
public sealed record RollupBatch(
    SiteId SiteId,
    DateOnly Date,
    DailySiteStats SiteStats,
    IReadOnlyList<DailyPageRow> Pages,
    IReadOnlyList<DailySourceRow> Sources,
    IReadOnlyList<DailyGeoRow> Geo,
    IReadOnlyList<DailyDeviceRow> Devices,
    IReadOnlyList<DailyEventRow> Events);
