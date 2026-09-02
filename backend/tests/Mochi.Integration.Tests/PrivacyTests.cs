using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Mochi.Integration.Tests;

public class PrivacyTests(MochiApiFixture fx) : IClassFixture<MochiApiFixture>
{
    private async Task<string> RegisterSiteWithTrafficAsync()
    {
        var resp = await fx.Client.PostAsJsonAsync("/api/sites",
            new { name = "privacy", domain = "privacy.example.com", timezone = "Europe/Berlin" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/collect")
        {
            Content = new StringContent($$"""{"site":"{{id}}","type":"pageview","path":"/hello"}""", Encoding.UTF8, "text/plain"),
        };
        req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; rv:130.0) Gecko/20100101 Firefox/130.0");
        await fx.Client.SendAsync(req);
        return id;
    }

    [Fact]
    public async Task Privacy_summary_reports_live_numbers()
    {
        var id = await RegisterSiteWithTrafficAsync();

        var summary = await fx.Client.GetFromJsonAsync<JsonElement>($"/api/sites/{id}/privacy");

        Assert.Equal("unlimited", summary.GetProperty("retention").GetString());
        Assert.Equal(7, summary.GetProperty("rawEventLifetimeDays").GetInt32());
        Assert.Equal(1, summary.GetProperty("rawEventsHeld").GetInt64());
    }

    [Fact]
    public async Task Export_returns_zip_with_all_aggregate_tables()
    {
        var id = await RegisterSiteWithTrafficAsync();
        // Roll up today so the export has rows.
        await fx.Client.PostAsync($"/api/admin/rollup/{DateTime.UtcNow:yyyy-MM-dd}", null);

        var resp = await fx.Client.GetAsync($"/api/sites/{id}/export");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/zip", resp.Content.Headers.ContentType?.MediaType);

        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        var names = zip.Entries.Select(e => e.Name).ToHashSet();
        foreach (var expected in new[] { "readme.txt", "daily_site_stats.csv", "daily_pages.csv", "daily_sources.csv", "daily_geo.csv", "daily_devices.csv", "daily_events.csv", "goals.csv" })
        {
            Assert.Contains(expected, names);
        }

        using var reader = new StreamReader(zip.GetEntry("daily_pages.csv")!.Open());
        var pagesCsv = await reader.ReadToEndAsync();
        Assert.Contains("/hello", pagesCsv);
        Assert.StartsWith("date,path,visitors", pagesCsv);
    }
}
