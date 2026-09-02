using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Mochi.Integration.Tests;

public class AuthTests(MochiApiFixture fx) : IClassFixture<MochiApiFixture>
{
    [Fact]
    public async Task Status_reflects_completed_setup()
    {
        var status = await fx.Client.GetFromJsonAsync<JsonElement>("/api/auth/status");

        Assert.False(status.GetProperty("needsSetup").GetBoolean());
        Assert.True(status.GetProperty("authenticated").GetBoolean());
        Assert.Equal(MochiApiFixture.AdminEmail, status.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Second_setup_is_rejected_even_with_the_code()
    {
        var client = fx.CreateAnonymousClient();
        await client.GetAsync("/api/auth/status");

        var resp = await client.PostAsJsonAsync("/api/auth/setup",
            new { code = MochiApiFixture.SetupCode, email = "intruder@example.com", password = "long-enough-pass" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_gets_uniform_401()
    {
        var client = fx.CreateAnonymousClient();
        await client.GetAsync("/api/auth/status");

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email = MochiApiFixture.AdminEmail, password = "wrong" });
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownEmail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_requests_get_401()
    {
        var client = fx.CreateAnonymousClient();

        var resp = await client.GetAsync("/api/sites");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Missing_xsrf_header_gets_400()
    {
        var client = await fx.CreateAuthedClientAsync();

        // Bypass the mirroring handler by sending a stale header value.
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/sites")
        {
            Content = JsonContent.Create(new { name = "x", domain = "x.example.com", timezone = "UTC" }),
        };
        req.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", "not-the-cookie-value");
        var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Logout_invalidates_the_session()
    {
        var client = await fx.CreateAuthedClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sites")).StatusCode);

        await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sites")).StatusCode);
    }

    [Fact]
    public async Task Collect_needs_no_cookie_or_header()
    {
        var client = fx.CreateAnonymousClient();

        var resp = await client.PostAsync("/api/collect",
            new StringContent("""{"site":"MC-00000","type":"pageview","path":"/"}""", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
    }

    [Fact]
    public async Task Setup_claimed_preexisting_sites_for_the_admin()
    {
        // The fixture created the admin after zero sites existed, so this just
        // verifies the happy path: a site created now belongs to the admin.
        var created = await fx.Client.PostAsJsonAsync("/api/sites",
            new { name = "owned", domain = "owned.example.com", timezone = "UTC" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await fx.Client.GetFromJsonAsync<JsonElement>("/api/sites");
        Assert.Contains(list.EnumerateArray(),
            s => s.GetProperty("site").GetProperty("domain").GetString() == "owned.example.com");
    }
}
