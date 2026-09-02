using System.Net;

namespace Mochi.Integration.Tests;

public class TrackingScriptTests(MochiApiFixture fx) : IClassFixture<MochiApiFixture>
{
    [Fact]
    public async Task Script_is_served_as_javascript()
    {
        var resp = await fx.Client.GetAsync("/script.js");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/javascript", resp.Content.Headers.ContentType?.MediaType);
        Assert.Contains("max-age=86400", resp.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Script_stays_under_two_kilobytes()
    {
        var bytes = await fx.Client.GetByteArrayAsync("/script.js");

        Assert.InRange(bytes.Length, 1, 2048);
    }

    [Fact]
    public async Task Script_never_touches_cookies_or_storage()
    {
        var source = await fx.Client.GetStringAsync("/script.js");

        Assert.DoesNotContain("document.cookie", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localStorage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("sessionStorage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("indexedDB", source, StringComparison.OrdinalIgnoreCase);
    }
}
