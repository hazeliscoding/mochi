using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.InMemory;

/// <summary>Reads rollups straight from the in-memory store's batches.</summary>
public sealed class InMemoryRollupReader(InMemoryRollupStore store) : IRollupReader
{
    private IEnumerable<RollupBatch> Range(SiteId siteId, DateOnly from, DateOnly to)
        => store.Batches.Where(b => b.SiteId == siteId && b.Date >= from && b.Date <= to);

    /// <inheritdoc />
    public Task<IReadOnlyList<DatedSiteStats>> SiteStatsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DatedSiteStats>>(
            [.. Range(siteId, from, to).OrderBy(b => b.Date).Select(b => new DatedSiteStats(b.Date, b.SiteStats))]);

    /// <inheritdoc />
    public Task<IReadOnlyList<DailyPageRow>> PagesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailyPageRow>>([.. Range(siteId, from, to).SelectMany(b => b.Pages)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<DailySourceRow>> SourcesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailySourceRow>>([.. Range(siteId, from, to).SelectMany(b => b.Sources)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<DailyGeoRow>> GeoAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailyGeoRow>>([.. Range(siteId, from, to).SelectMany(b => b.Geo)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<DailyDeviceRow>> DevicesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailyDeviceRow>>([.. Range(siteId, from, to).SelectMany(b => b.Devices)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<DailyEventRow>> EventsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DailyEventRow>>([.. Range(siteId, from, to).SelectMany(b => b.Events)]);
}
