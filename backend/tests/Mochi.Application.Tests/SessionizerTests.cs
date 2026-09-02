using Mochi.Application.Rollups;
using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Application.Tests;

public class SessionizerTests
{
    private static readonly DateOnly Day = new(2026, 9, 1);
    private static readonly DateTimeOffset T0 = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    private static SiteId Site()
    {
        Assert.True(SiteId.TryParse("MC-7F3K2", out var id));
        return id;
    }

    private static AnalyticsEvent Pv(ulong visitor, string path, int minutesAfterT0, string? referrerDomain = null, Channel channel = Channel.Direct)
        => AnalyticsEvent.Pageview(Site(), VisitorHash.FromValue(visitor), path, referrerDomain, channel, null, "DE", DeviceClass.Desktop, "Firefox", "Windows", T0.AddMinutes(minutesAfterT0));

    private static AnalyticsEvent Ev(ulong visitor, string name, string path, int minutesAfterT0)
        => AnalyticsEvent.Custom(Site(), VisitorHash.FromValue(visitor), name, path, null, Channel.Direct, null, "DE", DeviceClass.Desktop, "Firefox", "Windows", T0.AddMinutes(minutesAfterT0));

    [Fact]
    public void Splits_sessions_on_gaps_over_30_minutes()
    {
        var batch = Sessionizer.Roll(Site(), Day, [Pv(1, "/", 0), Pv(1, "/blog", 10), Pv(1, "/", 90)]);

        Assert.Equal(2, batch.SiteStats.Sessions);
        Assert.Equal(1, batch.SiteStats.Visitors);
        Assert.Equal(3, batch.SiteStats.Pageviews);
    }

    [Fact]
    public void Single_pageview_session_is_a_bounce()
    {
        var batch = Sessionizer.Roll(Site(), Day, [Pv(1, "/", 0), Pv(2, "/", 0), Pv(2, "/blog", 5)]);

        Assert.Equal(1, batch.SiteStats.BouncedSessions);
        Assert.Equal(2, batch.SiteStats.Sessions);
    }

    [Fact]
    public void Entries_and_exits_come_from_first_and_last_pageview()
    {
        var batch = Sessionizer.Roll(Site(), Day, [Pv(1, "/", 0), Pv(1, "/blog", 5), Pv(1, "/about", 10)]);

        var home = batch.Pages.Single(p => p.Path == "/");
        var about = batch.Pages.Single(p => p.Path == "/about");
        Assert.Equal(1, home.Entries);
        Assert.Equal(0, home.Exits);
        Assert.Equal(1, about.Exits);
        Assert.Equal(0, about.Entries);
    }

    [Fact]
    public void Session_duration_is_last_minus_first()
    {
        var batch = Sessionizer.Roll(Site(), Day, [Pv(1, "/", 0), Pv(1, "/blog", 12)]);

        Assert.Equal(720, batch.SiteStats.TotalSessionDurationSec);
    }

    [Fact]
    public void Sessions_are_attributed_to_first_event_source()
    {
        var batch = Sessionizer.Roll(Site(), Day,
            [Pv(1, "/", 0, "news.ycombinator.com", Channel.Social), Pv(1, "/blog", 5)]);

        var source = Assert.Single(batch.Sources);
        Assert.Equal(Channel.Social, source.Channel);
        Assert.Equal("news.ycombinator.com", source.ReferrerDomain);
        Assert.Equal(2, source.Pageviews);
    }

    [Fact]
    public void Custom_events_roll_up_with_unique_visitors()
    {
        var batch = Sessionizer.Roll(Site(), Day,
            [Ev(1, "signup", "/pricing", 0), Ev(1, "signup", "/pricing", 5), Ev(2, "signup", "/pricing", 3)]);

        var row = Assert.Single(batch.Events);
        Assert.Equal(3, row.Total);
        Assert.Equal(2, row.UniqueVisitors);
    }

    [Fact]
    public void Empty_day_produces_zeroed_stats()
    {
        var batch = Sessionizer.Roll(Site(), Day, []);

        Assert.Equal(0, batch.SiteStats.Visitors);
        Assert.Equal(0, batch.SiteStats.Sessions);
        Assert.Empty(batch.Pages);
    }
}
