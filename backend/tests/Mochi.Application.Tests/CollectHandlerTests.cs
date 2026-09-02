using Mochi.Application.Abstractions;
using Mochi.Application.Collect;
using Mochi.Application.Sites;
using Mochi.Domain.Collection;
using Mochi.Infrastructure.Collection;
using Mochi.Infrastructure.InMemory;

namespace Mochi.Application.Tests;

public class CollectHandlerTests
{
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
    }

    private readonly InMemorySiteRepository _sites = new();
    private readonly InMemoryAnalyticsEventStore _events = new();
    private readonly CollectHandler _handler;
    private readonly RegisterSiteHandler _register;

    public CollectHandlerTests()
    {
        var clock = new FixedClock();
        _handler = new CollectHandler(_sites, _events, new RotatingDailySaltProvider(clock), new UaParserUserAgentParser(), new NullGeoLocator(), clock);
        _register = new RegisterSiteHandler(_sites, clock);
    }

    private CollectCommand Beacon(string siteId, string type = "pageview", string? path = "/blog?utm_campaign=launch&x=1", string? name = null) =>
        new(siteId, type, path, name, "https://news.ycombinator.com/item?id=123", "203.0.113.7", "Mozilla/5.0 (Windows NT 10.0; rv:130.0) Gecko/20100101 Firefox/130.0");

    [Fact]
    public async Task Unknown_site_is_dropped()
    {
        var result = await _handler.HandleAsync(Beacon("MC-7F3K2"));

        Assert.False(result.Stored);
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Pageview_is_scrubbed_and_stored()
    {
        var site = await _register.HandleAsync("test", "example.com", "Europe/Berlin");
        var result = await _handler.HandleAsync(Beacon(site.Id.Value));

        Assert.True(result.Stored);
        var evt = Assert.Single(_events.Events);
        Assert.Equal("/blog", evt.Path);
        Assert.Equal("launch", evt.Campaign);
        Assert.Equal(Channel.Social, evt.Channel);
        Assert.Equal("news.ycombinator.com", evt.ReferrerDomain);
        Assert.Equal("Firefox", evt.Browser);
        Assert.Equal("Windows", evt.Os);
    }

    [Fact]
    public async Task Custom_event_without_name_is_dropped()
    {
        var site = await _register.HandleAsync("test", "example.com", "Europe/Berlin");
        var result = await _handler.HandleAsync(Beacon(site.Id.Value, type: "event"));

        Assert.False(result.Stored);
        Assert.Empty(_events.Events);
    }

    [Fact]
    public async Task Same_visitor_hashes_identically_within_a_day()
    {
        var site = await _register.HandleAsync("test", "example.com", "Europe/Berlin");
        await _handler.HandleAsync(Beacon(site.Id.Value));
        await _handler.HandleAsync(Beacon(site.Id.Value, path: "/about"));

        var hashes = _events.Events.Select(e => e.Visitor).Distinct();
        Assert.Single(hashes);
    }
}
