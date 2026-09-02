using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mochi.Api.Contracts;
using Mochi.Application.Abstractions;
using Mochi.Application.Collect;
using Mochi.Application.Rollups;
using Mochi.Application.Sites;
using Mochi.Domain.Sites;
using Mochi.Infrastructure;
using Mochi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMochi(builder.Configuration);
var app = builder.Build();

if (!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Mochi")))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MochiDbContext>().Database.MigrateAsync();
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// Ingestion. Body is text/plain JSON so browsers skip the CORS preflight.
// Do not "fix" the content type; every analytics vendor does this (ADR 0002).
// Invalid-but-parseable payloads still get 202 so probes and blockers learn
// nothing; drop reasons go to the log only. Malformed JSON is the one 400.
app.MapPost("/api/collect", async (HttpContext http, CollectHandler handler, ILogger<Program> log, CancellationToken ct) =>
{
    http.Response.Headers.AccessControlAllowOrigin = "*";

    CollectPayload? payload;
    try
    {
        using var reader = new StreamReader(http.Request.Body);
        payload = JsonSerializer.Deserialize<CollectPayload>(await reader.ReadToEndAsync(ct), jsonOptions);
    }
    catch (JsonException)
    {
        return Results.BadRequest();
    }

    if (payload is null) return Results.BadRequest();

    var command = new CollectCommand(
        payload.Site ?? string.Empty,
        payload.Type ?? string.Empty,
        payload.Path,
        payload.Name,
        payload.Referrer,
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        http.Request.Headers.UserAgent.ToString());

    var result = await handler.HandleAsync(command, ct);
    if (!result.Stored) log.LogInformation("collect drop: {Reason}", result.DropReason);

    return Results.Accepted();
});

// Site management. Dashboard-only; auth arrives in v0.5, so bind to localhost
// until then.
var snippetBaseUrl = app.Configuration["Mochi:SnippetBaseUrl"] ?? "http://localhost:5000";

app.MapPost("/api/sites", async (SiteRequest req, RegisterSiteHandler handler, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Domain) || string.IsNullOrWhiteSpace(req.Timezone))
        return Results.UnprocessableEntity("name, domain and timezone are required");

    var site = await handler.HandleAsync(req.Name, req.Domain, req.Timezone, ct);
    return Results.Created($"/api/sites/{site.Id.Value}", SiteResponse.From(site, snippetBaseUrl));
});

app.MapGet("/api/sites", async (ISiteRepository sites, CancellationToken ct) =>
{
    var all = await sites.ListAsync(ct);
    return Results.Ok(all.Select(s => SiteResponse.From(s, snippetBaseUrl)));
});

app.MapGet("/api/sites/{id}", async (string id, ISiteRepository sites, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    var site = await sites.GetAsync(siteId, ct);
    return site is null ? Results.NotFound() : Results.Ok(SiteResponse.From(site, snippetBaseUrl));
});

app.MapPut("/api/sites/{id}", async (string id, SiteRequest req, ISiteRepository sites, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    var site = await sites.GetAsync(siteId, ct);
    if (site is null) return Results.NotFound();

    var retention = SiteResponse.ParseRetention(req.Retention) ?? site.Retention;
    site.UpdateSettings(req.Name ?? site.Name, req.Timezone ?? site.Timezone, retention);
    await sites.UpdateAsync(site, ct);
    return Results.Ok(SiteResponse.From(site, snippetBaseUrl));
});

// Deleting a site deletes all its data immediately: raw events, rollups, then
// the site row. The Privacy Center promise depends on this (ADR 0002).
app.MapDelete("/api/sites/{id}", async (string id, ISiteRepository sites, IAnalyticsEventStore events, IRollupStore rollups, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    await events.PurgeSiteAsync(siteId, ct);
    await rollups.PurgeSiteAsync(siteId, ct);
    await sites.RemoveAsync(siteId, ct);
    return Results.NoContent();
});

// Manual rollup rerun per ADR 0003. Unauthenticated until v0.5, so keep the
// API bound to localhost in the meantime.
app.MapPost("/api/admin/rollup/{date}", async (string date, RollupJob job, CancellationToken ct) =>
{
    if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var day)) return Results.BadRequest("date must be yyyy-MM-dd");
    await job.RunForDayAsync(day, ct);
    return Results.Ok();
});

app.Run();

/// <summary>Marker so WebApplicationFactory can host the app in integration tests.</summary>
public partial class Program;
