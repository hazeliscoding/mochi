using Microsoft.EntityFrameworkCore;
using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Postgres-backed rollup store. Writes are delete-and-rewrite per site and day.</summary>
public sealed class EfRollupStore(MochiDbContext db) : IRollupStore
{
    /// <inheritdoc />
    public async Task ReplaceDayAsync(RollupBatch batch, CancellationToken ct = default)
    {
        var site = batch.SiteId.Value;
        var date = batch.Date;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.SiteStats.Where(r => r.SiteId == site && r.Date == date).ExecuteDeleteAsync(ct);
        await db.PageRows.Where(r => r.SiteId == site && r.Date == date).ExecuteDeleteAsync(ct);
        await db.SourceRows.Where(r => r.SiteId == site && r.Date == date).ExecuteDeleteAsync(ct);
        await db.GeoRows.Where(r => r.SiteId == site && r.Date == date).ExecuteDeleteAsync(ct);
        await db.DeviceRows.Where(r => r.SiteId == site && r.Date == date).ExecuteDeleteAsync(ct);
        await db.EventRows.Where(r => r.SiteId == site && r.Date == date).ExecuteDeleteAsync(ct);

        db.SiteStats.Add(new SiteStatsRow
        {
            SiteId = site,
            Date = date,
            Visitors = batch.SiteStats.Visitors,
            Pageviews = batch.SiteStats.Pageviews,
            Sessions = batch.SiteStats.Sessions,
            BouncedSessions = batch.SiteStats.BouncedSessions,
            TotalSessionDurationSec = batch.SiteStats.TotalSessionDurationSec,
        });
        db.PageRows.AddRange(batch.Pages.Select(p => new PageRow
        {
            SiteId = site,
            Date = date,
            Path = p.Path,
            Visitors = p.Visitors,
            Pageviews = p.Pageviews,
            Entries = p.Entries,
            Exits = p.Exits,
            BouncedSessions = p.BouncedSessions,
            TotalDurationSec = p.TotalDurationSec,
        }));
        db.SourceRows.AddRange(batch.Sources.Select(s => new SourceRow
        {
            SiteId = site,
            Date = date,
            Channel = (short)s.Channel,
            ReferrerDomain = s.ReferrerDomain ?? string.Empty,
            Campaign = s.Campaign ?? string.Empty,
            Visitors = s.Visitors,
            Pageviews = s.Pageviews,
            BouncedSessions = s.BouncedSessions,
        }));
        db.GeoRows.AddRange(batch.Geo.Select(g => new GeoRow
        {
            SiteId = site,
            Date = date,
            Country = g.Country,
            Visitors = g.Visitors,
        }));
        db.DeviceRows.AddRange(batch.Devices.Select(d => new DeviceRow
        {
            SiteId = site,
            Date = date,
            DeviceClass = (short)d.Device,
            Browser = d.Browser,
            Os = d.Os,
            Visitors = d.Visitors,
        }));
        db.EventRows.AddRange(batch.Events.Select(ev => new EventRow
        {
            SiteId = site,
            Date = date,
            EventName = ev.EventName,
            Path = ev.Path,
            Channel = (short)ev.Channel,
            Total = ev.Total,
            UniqueVisitors = ev.UniqueVisitors,
        }));

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task PurgeBeforeAsync(SiteId siteId, DateOnly cutoff, CancellationToken ct = default)
    {
        var site = siteId.Value;
        await db.SiteStats.Where(r => r.SiteId == site && r.Date < cutoff).ExecuteDeleteAsync(ct);
        await db.PageRows.Where(r => r.SiteId == site && r.Date < cutoff).ExecuteDeleteAsync(ct);
        await db.SourceRows.Where(r => r.SiteId == site && r.Date < cutoff).ExecuteDeleteAsync(ct);
        await db.GeoRows.Where(r => r.SiteId == site && r.Date < cutoff).ExecuteDeleteAsync(ct);
        await db.DeviceRows.Where(r => r.SiteId == site && r.Date < cutoff).ExecuteDeleteAsync(ct);
        await db.EventRows.Where(r => r.SiteId == site && r.Date < cutoff).ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task PurgeSiteAsync(SiteId siteId, CancellationToken ct = default)
    {
        var site = siteId.Value;
        await db.SiteStats.Where(r => r.SiteId == site).ExecuteDeleteAsync(ct);
        await db.PageRows.Where(r => r.SiteId == site).ExecuteDeleteAsync(ct);
        await db.SourceRows.Where(r => r.SiteId == site).ExecuteDeleteAsync(ct);
        await db.GeoRows.Where(r => r.SiteId == site).ExecuteDeleteAsync(ct);
        await db.DeviceRows.Where(r => r.SiteId == site).ExecuteDeleteAsync(ct);
        await db.EventRows.Where(r => r.SiteId == site).ExecuteDeleteAsync(ct);
    }
}
