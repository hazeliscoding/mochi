using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Mochi.Integration.Tests;

public class GoalTests(MochiApiFixture fx) : IClassFixture<MochiApiFixture>
{
    private async Task<string> RegisterSiteWithTrafficAsync()
    {
        var resp = await fx.Client.PostAsJsonAsync("/api/sites",
            new { name = "goals", domain = "goals.example.com", timezone = "Europe/Berlin" });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        foreach (var payload in new[]
        {
            $$"""{"site":"{{id}}","type":"pageview","path":"/pricing"}""",
            $$"""{"site":"{{id}}","type":"event","name":"signup","path":"/pricing"}""",
        })
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/collect")
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/plain"),
            };
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; rv:130.0) Gecko/20100101 Firefox/130.0");
            await fx.Client.SendAsync(req);
        }

        return id;
    }

    [Fact]
    public async Task Goal_crud_roundtrips()
    {
        var siteId = await RegisterSiteWithTrafficAsync();

        var created = await fx.Client.PostAsJsonAsync($"/api/sites/{siteId}/goals",
            new { name = "Signed up", type = "event", target = "signup" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var goal = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("g_", goal.GetProperty("id").GetString());

        var list = await fx.Client.GetFromJsonAsync<JsonElement>($"/api/sites/{siteId}/goals");
        Assert.Single(list.EnumerateArray());

        var del = await fx.Client.DeleteAsync($"/api/sites/{siteId}/goals/{goal.GetProperty("id").GetString()}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        list = await fx.Client.GetFromJsonAsync<JsonElement>($"/api/sites/{siteId}/goals");
        Assert.Empty(list.EnumerateArray());
    }

    [Fact]
    public async Task Goal_stats_match_events_and_pages_retroactively()
    {
        var siteId = await RegisterSiteWithTrafficAsync();

        // Created after the traffic arrived; stats must still show conversions.
        await fx.Client.PostAsJsonAsync($"/api/sites/{siteId}/goals",
            new { name = "Signed up", type = "event", target = "signup" });
        await fx.Client.PostAsJsonAsync($"/api/sites/{siteId}/goals",
            new { name = "Saw pricing", type = "page", target = "/pricing" });

        var stats = await fx.Client.GetFromJsonAsync<JsonElement>($"/api/sites/{siteId}/goals/stats");

        var byName = stats.EnumerateArray().ToDictionary(g => g.GetProperty("name").GetString()!);
        Assert.Equal(1, byName["Signed up"].GetProperty("conversions").GetInt32());
        Assert.Equal(100, byName["Signed up"].GetProperty("ratePct").GetDouble());
        Assert.Equal(1, byName["Saw pricing"].GetProperty("conversions").GetInt32());
    }

    [Fact]
    public async Task Goal_with_unknown_type_is_rejected()
    {
        var siteId = await RegisterSiteWithTrafficAsync();

        var resp = await fx.Client.PostAsJsonAsync($"/api/sites/{siteId}/goals",
            new { name = "Bad", type = "clicks", target = "x" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }
}
