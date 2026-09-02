using Microsoft.EntityFrameworkCore;
using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Postgres-backed rollup reader. Maps row entities back to application records.</summary>
public sealed class EfRollupReader(MochiDbContext db) : IRollupReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DatedSiteStats>> SiteStatsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.SiteStats.Where(r => r.SiteId == siteId.Value && r.Date >= from && r.Date <= to)
            .OrderBy(r => r.Date)
            .Select(r => new DatedSiteStats(r.Date, new DailySiteStats(r.Visitors, r.Pageviews, r.Sessions, r.BouncedSessions, r.TotalSessionDurationSec)))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<DateOnly?> OldestDateAsync(SiteId siteId, CancellationToken ct = default)
        => await db.SiteStats.Where(r => r.SiteId == siteId.Value)
            .OrderBy(r => r.Date)
            .Select(r => (DateOnly?)r.Date)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dated<DailyPageRow>>> PagesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.PageRows.Where(r => r.SiteId == siteId.Value && r.Date >= from && r.Date <= to)
            .Select(r => new Dated<DailyPageRow>(r.Date, new DailyPageRow(r.Path, r.Visitors, r.Pageviews, r.Entries, r.Exits, r.BouncedSessions, r.TotalDurationSec)))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dated<DailySourceRow>>> SourcesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.SourceRows.Where(r => r.SiteId == siteId.Value && r.Date >= from && r.Date <= to)
            .Select(r => new Dated<DailySourceRow>(r.Date, new DailySourceRow(
                (Channel)r.Channel,
                r.ReferrerDomain == "" ? null : r.ReferrerDomain,
                r.Campaign == "" ? null : r.Campaign,
                r.Visitors, r.Pageviews, r.BouncedSessions)))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dated<DailyGeoRow>>> GeoAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.GeoRows.Where(r => r.SiteId == siteId.Value && r.Date >= from && r.Date <= to)
            .Select(r => new Dated<DailyGeoRow>(r.Date, new DailyGeoRow(r.Country, r.Visitors)))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dated<DailyDeviceRow>>> DevicesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.DeviceRows.Where(r => r.SiteId == siteId.Value && r.Date >= from && r.Date <= to)
            .Select(r => new Dated<DailyDeviceRow>(r.Date, new DailyDeviceRow((DeviceClass)r.DeviceClass, r.Browser, r.Os, r.Visitors)))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Dated<DailyEventRow>>> EventsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.EventRows.Where(r => r.SiteId == siteId.Value && r.Date >= from && r.Date <= to)
            .Select(r => new Dated<DailyEventRow>(r.Date, new DailyEventRow(r.EventName, r.Path, (Channel)r.Channel, r.Total, r.UniqueVisitors)))
            .ToListAsync(ct);
}

