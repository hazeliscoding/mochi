using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Application.Sites;
using Mochi.Domain.Collection;
using Mochi.Domain.Sites;
using Mochi.Infrastructure.InMemory;

namespace Mochi.Application.Tests;

public class RollupJobTests
{
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
    }

    private static readonly DateOnly Today = new(2026, 9, 2);

    private readonly InMemorySiteRepository _sites = new();
    private readonly InMemoryAnalyticsEventStore _events = new();
    private readonly InMemoryRollupStore _rollups = new();
    private readonly FixedClock _clock = new();
    private readonly RollupJob _job;

    public RollupJobTests()
    {
        _job = new RollupJob(_sites, _events, _rollups, _clock);
    }

    private async Task<Site> RegisterAsync(RetentionPolicy retention)
    {
        var site = await new RegisterSiteHandler(_sites, _clock).HandleAsync("t", "t.example.com", "UTC");
        site.UpdateSettings(site.Name, site.Timezone, retention);
        return site;
    }

    private static RollupBatch EmptyBatch(SiteId siteId, DateOnly date)
        => Sessionizer.Roll(siteId, date, []);

    [Fact]
    public async Task Retention_purge_drops_rollups_past_the_cutoff()
    {
        var site = await RegisterAsync(RetentionPolicy.Days30);
        await _rollups.ReplaceDayAsync(EmptyBatch(site.Id, Today.AddDays(-40)));
        await _rollups.ReplaceDayAsync(EmptyBatch(site.Id, Today.AddDays(-10)));

        await _job.RunForSiteAsync(site, Today.AddDays(-1));

        var dates = _rollups.Batches.Where(b => b.SiteId == site.Id).Select(b => b.Date).ToList();
        Assert.DoesNotContain(Today.AddDays(-40), dates);
        Assert.Contains(Today.AddDays(-10), dates);
    }

    [Fact]
    public async Task Unlimited_retention_purges_nothing()
    {
        var site = await RegisterAsync(RetentionPolicy.Unlimited);
        await _rollups.ReplaceDayAsync(EmptyBatch(site.Id, Today.AddYears(-3)));

        await _job.RunForSiteAsync(site, Today.AddDays(-1));

        Assert.Contains(_rollups.Batches, b => b.SiteId == site.Id && b.Date == Today.AddYears(-3));
    }

    [Fact]
    public async Task Raw_events_older_than_seven_days_are_purged()
    {
        var site = await RegisterAsync(RetentionPolicy.Unlimited);
        var old = AnalyticsEvent.Pageview(site.Id, VisitorHash.FromValue(1), "/", null, Channel.Direct, null, null,
            DeviceClass.Desktop, "Firefox", "Windows", _clock.UtcNow.AddDays(-8));
        var fresh = AnalyticsEvent.Pageview(site.Id, VisitorHash.FromValue(2), "/", null, Channel.Direct, null, null,
            DeviceClass.Desktop, "Firefox", "Windows", _clock.UtcNow.AddDays(-2));
        await _events.AppendAsync(old);
        await _events.AppendAsync(fresh);

        await _job.RunForDayAsync(Today.AddDays(-1));

        var held = _events.Events.Select(e => e.Visitor.Value).ToList();
        Assert.DoesNotContain(1UL, held);
        Assert.Contains(2UL, held);
    }
}
