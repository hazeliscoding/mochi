using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Mochi.Integration.Tests;

/// <summary>
/// One Postgres container and one hosted API shared by all integration tests.
/// The app applies migrations on startup, so the schema is real.
/// </summary>
public sealed class MochiApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private WebApplicationFactory<Program>? _factory;

    public string ConnectionString => _postgres.GetConnectionString();

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
        });
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
