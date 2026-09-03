using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Mochi.Integration.Tests;

/// <summary>Fixture with limits low enough to trip inside a test.</summary>
public sealed class TightLimitsFixture : MochiApiFixture
{
    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> ExtraSettings { get; } = new Dictionary<string, string>
    {
        ["Mochi:RateLimits:AuthPerMinute"] = "3",
        ["Mochi:RateLimits:CollectPerMinute"] = "5",
    };
}

public class RateLimitTests(TightLimitsFixture fx) : IClassFixture<TightLimitsFixture>
{
    [Fact]
    public async Task Login_attempts_beyond_the_window_get_429()
    {
        var client = fx.CreateAnonymousClient();
        await client.GetAsync("/api/auth/status");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login", new { email = "x@example.com", password = "wrong" });
            statuses.Add(resp.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.DoesNotContain(HttpStatusCode.OK, statuses);
    }

    [Fact]
    public async Task Collect_flood_beyond_the_window_gets_429()
    {
        var client = fx.CreateAnonymousClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 8; i++)
        {
            var resp = await client.PostAsync("/api/collect",
                new StringContent("""{"site":"MC-00000","type":"pageview","path":"/"}""", Encoding.UTF8, "text/plain"));
            statuses.Add(resp.StatusCode);
        }

        Assert.Contains(HttpStatusCode.Accepted, statuses);
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}
