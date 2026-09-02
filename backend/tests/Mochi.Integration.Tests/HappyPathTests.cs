using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Mochi.Integration.Tests;

public class HappyPathTests(MochiApiFixture fx) : IClassFixture<MochiApiFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<JsonElement> RegisterSiteAsync(string domain)
    {
        var resp = await fx.Client.PostAsJsonAsync("/api/sites",
            new { name = domain, domain, timezone = "Europe/Berlin" });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private Task<HttpResponseMessage> CollectAsync(object payload)
        => fx.Client.PostAsync("/api/collect",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "text/plain"));

    private async Task<T?> ScalarAsync<T>(string sql, string siteId)
    {
        await using var conn = new NpgsqlConnection(fx.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("site", siteId);
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)result;
    }

    [Fact]
    public async Task Register_site_returns_id_and_snippet()
    {
        var site = await RegisterSiteAsync("register.example.com");

        Assert.Matches("^MC-[0-9A-Z]{5}$", site.GetProperty("id").GetString());
        Assert.Contains("data-site=", site.GetProperty("snippet").GetString());
        Assert.Equal("unlimited", site.GetProperty("retention").GetString());
    }

    [Fact]
    public async Task Collect_accepts_pageview_with_open_cors()
    {
        var site = await RegisterSiteAsync("collect.example.com");
        var id = site.GetProperty("id").GetString();

        var resp = await CollectAsync(new { site = id, type = "pageview", path = "/blog", referrer = "https://duckduckgo.com/" });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
        Assert.Equal("*", resp.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal(1L, await ScalarAsync<long>("SELECT COUNT(*) FROM events WHERE site_id = @site", id!));
    }

    [Fact]
    public async Task Collect_drops_unknown_site_but_still_accepts()
    {
        var resp = await CollectAsync(new { site = "MC-00000", type = "pageview", path = "/" });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM events WHERE site_id = @site", "MC-00000"));
    }

    [Fact]
    public async Task Collect_rejects_malformed_json()
    {
        var resp = await fx.Client.PostAsync("/api/collect", new StringContent("not json", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Rollup_writes_daily_rows_for_collected_events()
    {
        var site = await RegisterSiteAsync("rollup.example.com");
        var id = site.GetProperty("id").GetString();

        await CollectAsync(new { site = id, type = "pageview", path = "/", referrer = "https://news.ycombinator.com/item?id=1" });
        await CollectAsync(new { site = id, type = "pageview", path = "/blog" });
        await CollectAsync(new { site = id, type = "event", name = "signup", path = "/pricing" });

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var resp = await fx.Client.PostAsync($"/api/admin/rollup/{today}", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        Assert.Equal(2, await ScalarAsync<int>("SELECT pageviews FROM daily_site_stats WHERE site_id = @site", id!));
        Assert.Equal(2L, await ScalarAsync<long>("SELECT COUNT(*) FROM daily_pages WHERE site_id = @site", id!));
        Assert.Equal(1, await ScalarAsync<int>("SELECT total FROM daily_events WHERE site_id = @site AND event_name = 'signup'", id!));
    }

    [Fact]
    public async Task Delete_site_removes_site_events_and_rollups()
    {
        var site = await RegisterSiteAsync("delete.example.com");
        var id = site.GetProperty("id").GetString();
        await CollectAsync(new { site = id, type = "pageview", path = "/" });
        await fx.Client.PostAsync($"/api/admin/rollup/{DateTime.UtcNow:yyyy-MM-dd}", null);

        var del = await fx.Client.DeleteAsync($"/api/sites/{id}");

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fx.Client.GetAsync($"/api/sites/{id}")).StatusCode);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM events WHERE site_id = @site", id!));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT COUNT(*) FROM daily_site_stats WHERE site_id = @site", id!));
    }
}
