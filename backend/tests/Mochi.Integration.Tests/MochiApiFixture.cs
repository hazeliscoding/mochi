using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Mochi.Integration.Tests;

/// <summary>
/// One Postgres container and one hosted API shared by all tests of a class.
/// The app applies migrations on startup, so the schema is real. The fixture
/// runs first-run setup and exposes an authenticated admin client.
/// </summary>
public class MochiApiFixture : IAsyncLifetime
{
    /// <summary>Setup code injected via configuration.</summary>
    public const string SetupCode = "test-setup-code";

    /// <summary>Email of the admin account created by the fixture.</summary>
    public const string AdminEmail = "admin@example.com";

    /// <summary>Password of the admin account created by the fixture.</summary>
    public const string AdminPassword = "correct-horse-battery";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private WebApplicationFactory<Program>? _factory;

    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>Additional configuration for derived fixtures (e.g. tight rate limits).</summary>
    protected virtual IReadOnlyDictionary<string, string> ExtraSettings { get; } = new Dictionary<string, string>();

    /// <summary>Cookie-carrying client, already authenticated as the admin.</summary>
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment(Environments.Production);
            // UseSetting lands in configuration before Program.cs runs, which
            // AddMochi requires to pick the Postgres adapters.
            b.UseSetting("ConnectionStrings:Mochi", _postgres.GetConnectionString());
            b.UseSetting("MOCHI_SETUP_CODE", SetupCode);
            foreach (var (key, value) in ExtraSettings) b.UseSetting(key, value);
        });

        Client = CreateAnonymousClient();
        // Prime the XSRF cookie, then create the admin account. Setup logs in.
        await Client.GetAsync("/api/auth/status");
        var setup = await Client.PostAsJsonAsync("/api/auth/setup",
            new { code = SetupCode, email = AdminEmail, password = AdminPassword });
        if (setup.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"fixture setup failed: {setup.StatusCode}");
    }

    /// <summary>Fresh client with its own cookie jar and no session.</summary>
    public HttpClient CreateAnonymousClient()
    {
        var cookies = new CookieContainerHandler();
        return _factory!.CreateDefaultClient(new XsrfMirrorHandler(cookies.Container), cookies);
    }

    /// <summary>Fresh client logged in as the fixture's admin account.</summary>
    public async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = CreateAnonymousClient();
        await client.GetAsync("/api/auth/status");
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = AdminEmail, password = AdminPassword });
        if (login.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"fixture login failed: {login.StatusCode}");
        return client;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>Mirrors the XSRF-TOKEN cookie into the X-XSRF-TOKEN header, like Angular does.</summary>
    private sealed class XsrfMirrorHandler(CookieContainer cookies) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method != HttpMethod.Get && request.RequestUri is { } uri)
            {
                var token = cookies.GetCookies(uri)["XSRF-TOKEN"]?.Value;
                if (token is not null) request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", token);
            }

            return base.SendAsync(request, ct);
        }
    }
}
