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
    public Task<DateOnly?> OldestDateAsync(SiteId siteId, CancellationToken ct = default)
        => Task.FromResult(store.Batches.Where(b => b.SiteId == siteId).Select(b => (DateOnly?)b.Date).Min());

    /// <inheritdoc />
    public Task<IReadOnlyList<Dated<DailyPageRow>>> PagesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Dated<DailyPageRow>>>(
            [.. Range(siteId, from, to).SelectMany(b => b.Pages.Select(r => new Dated<DailyPageRow>(b.Date, r)))]);

    /// <inheritdoc />
    public Task<IReadOnlyList<Dated<DailySourceRow>>> SourcesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Dated<DailySourceRow>>>(
            [.. Range(siteId, from, to).SelectMany(b => b.Sources.Select(r => new Dated<DailySourceRow>(b.Date, r)))]);

    /// <inheritdoc />
    public Task<IReadOnlyList<Dated<DailyGeoRow>>> GeoAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Dated<DailyGeoRow>>>(
            [.. Range(siteId, from, to).SelectMany(b => b.Geo.Select(r => new Dated<DailyGeoRow>(b.Date, r)))]);

    /// <inheritdoc />
    public Task<IReadOnlyList<Dated<DailyDeviceRow>>> DevicesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Dated<DailyDeviceRow>>>(
            [.. Range(siteId, from, to).SelectMany(b => b.Devices.Select(r => new Dated<DailyDeviceRow>(b.Date, r)))]);

    /// <inheritdoc />
    public Task<IReadOnlyList<Dated<DailyEventRow>>> EventsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Dated<DailyEventRow>>>(
            [.. Range(siteId, from, to).SelectMany(b => b.Events.Select(r => new Dated<DailyEventRow>(b.Date, r)))]);
}
