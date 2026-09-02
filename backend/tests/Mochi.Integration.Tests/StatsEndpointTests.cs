using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Mochi.Integration.Tests;

public class StatsEndpointTests(MochiApiFixture fx) : IClassFixture<MochiApiFixture>, IAsyncLifetime
{
    private const string Firefox = "Mozilla/5.0 (Windows NT 10.0; rv:130.0) Gecko/20100101 Firefox/130.0";
    private const string Iphone = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1";

    private string _siteId = "";

    public async Task InitializeAsync()
    {
        var resp = await fx.Client.PostAsJsonAsync("/api/sites",
            new { name = "stats", domain = "stats.example.com", timezone = "Europe/Berlin" });
        var site = await resp.Content.ReadFromJsonAsync<JsonElement>();
        _siteId = site.GetProperty("id").GetString()!;

        // Visitor 1 (Firefox): two pageviews from Hacker News plus a signup.
        await CollectAsync(Firefox, """{"site":"__S__","type":"pageview","path":"/","referrer":"https://news.ycombinator.com/item?id=1"}""");
        await CollectAsync(Firefox, """{"site":"__S__","type":"pageview","path":"/pricing"}""");
        await CollectAsync(Firefox, """{"site":"__S__","type":"event","name":"signup","path":"/pricing"}""");
        // Visitor 2 (iPhone): bounces off the homepage.
        await CollectAsync(Iphone, """{"site":"__S__","type":"pageview","path":"/"}""");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task CollectAsync(string userAgent, string payload)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/collect")
        {
            Content = new StringContent(payload.Replace("__S__", _siteId), Encoding.UTF8, "text/plain"),
        };
        req.Headers.UserAgent.ParseAdd(userAgent);
        var resp = await fx.Client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    private Task<JsonElement> GetAsync(string path)
        => fx.Client.GetFromJsonAsync<JsonElement>($"/api/sites/{_siteId}/stats/{path}");

    [Fact]
    public async Task Summary_counts_todays_live_events()
    {
        var summary = await GetAsync("summary");
        var current = summary.GetProperty("current");

        Assert.Equal(2, current.GetProperty("visitors").GetInt32());
        Assert.Equal(3, current.GetProperty("pageviews").GetInt32());
        Assert.Equal(50, current.GetProperty("bounceRatePct").GetDouble());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("compare").ValueKind);
    }

    [Fact]
    public async Task Pages_rank_by_pageviews_with_entries_and_exits()
    {
        var pages = await GetAsync("pages");

        var home = pages.EnumerateArray().Single(p => p.GetProperty("path").GetString() == "/");
        Assert.Equal(2, home.GetProperty("visitors").GetInt32());
        Assert.Equal(2, home.GetProperty("entries").GetInt32());
    }

    [Fact]
    public async Task Sources_group_by_channel_and_referrer()
    {
        var channels = await GetAsync("sources?group=channels");
        var referrers = await GetAsync("sources?group=referrers");

        Assert.Contains(channels.EnumerateArray(), c => c.GetProperty("name").GetString() == "Social");
        var hn = Assert.Single(referrers.EnumerateArray());
        Assert.Equal("news.ycombinator.com", hn.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Events_report_totals_and_conversion()
    {
        var events = await GetAsync("events");

        var signup = Assert.Single(events.EnumerateArray());
        Assert.Equal("signup", signup.GetProperty("name").GetString());
        Assert.Equal(1, signup.GetProperty("total").GetInt32());
        Assert.Equal(50, signup.GetProperty("convPct").GetDouble());
    }

    [Fact]
    public async Task Realtime_reports_active_visitors_and_devices()
    {
        var rt = await GetAsync("realtime");

        Assert.Equal(2, rt.GetProperty("activeVisitors").GetInt32());
        Assert.Equal(30, rt.GetProperty("pageviewsPerMinute").GetArrayLength());
        Assert.Equal(1, rt.GetProperty("devices").GetProperty("desktop").GetInt32());
        Assert.Equal(1, rt.GetProperty("devices").GetProperty("mobile").GetInt32());
    }

    [Fact]
    public async Task Timeseries_returns_one_point_per_day()
    {
        var ts = await GetAsync("timeseries?metric=pageviews");

        Assert.Equal(30, ts.GetProperty("points").GetArrayLength());
        Assert.Equal(3, ts.GetProperty("points").EnumerateArray().Last().GetProperty("value").GetInt64());
    }

    [Fact]
    public async Task Site_list_carries_live_numbers()
    {
        var sites = await fx.Client.GetFromJsonAsync<JsonElement>("/api/sites");

        var mine = sites.EnumerateArray().Single(s => s.GetProperty("site").GetProperty("id").GetString() == _siteId);
        Assert.Equal(3, mine.GetProperty("viewsLast30d").GetInt64());
        Assert.Equal("active", mine.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Unknown_site_returns_404()
    {
        var resp = await fx.Client.GetAsync("/api/sites/MC-00000/stats/summary");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
