using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mochi.Api.Contracts;
using Mochi.Application.Abstractions;
using Mochi.Application.Collect;
using Mochi.Application.Rollups;
using Mochi.Application.Sites;
using Mochi.Application.Stats;
using Mochi.Domain.Sites;
using Mochi.Infrastructure;
using Mochi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMochi(builder.Configuration);
var app = builder.Build();

// Migrate on startup. Retries because the database container may still be
// initializing when the API starts (postgres restarts once during initdb).
if (!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Mochi")))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MochiDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            app.Logger.LogWarning("migration attempt {Attempt} failed: {Message}; retrying", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// Serves wwwroot/script.js, the embeddable tracking snippet. Cross-origin by
// nature (script tags are exempt from CORS); cached for a day so busy sites
// do not hammer the API, short enough that fixes roll out fast.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=86400",
});

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

app.MapGet("/api/sites", async (ISiteRepository sites, StatsService stats, CancellationToken ct) =>
{
    var items = new List<SiteListItem>();
    foreach (var s in await sites.ListAsync(ct))
    {
        var o = await stats.OverviewAsync(s.Id, ct);
        items.Add(new SiteListItem(SiteResponse.From(s, snippetBaseUrl), o.ViewsLast30d, o.ActiveNow, o.ViewsLast30d > 0 ? "active" : "waiting"));
    }

    return Results.Ok(items);
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

// Stats queries (ADR 0002). from/to are inclusive UTC days, defaulting to the
// last 30 days; compare is "previous", "year" or absent.
var stats = app.MapGroup("/api/sites/{id}/stats");

stats.MapGet("/summary", (string id, DateOnly? from, DateOnly? to, string? compare, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.SummaryAsync(siteId, From(from), To(to), compare, ct)));

stats.MapGet("/timeseries", (string id, DateOnly? from, DateOnly? to, string? metric, string? compare, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.TimeseriesAsync(siteId, From(from), To(to), metric ?? "visitors", compare, ct)));

stats.MapGet("/pages", (string id, DateOnly? from, DateOnly? to, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.PagesAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/sources", (string id, DateOnly? from, DateOnly? to, string? group, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.SourcesAsync(siteId, From(from), To(to), group ?? "channels", ct)));

stats.MapGet("/geo", (string id, DateOnly? from, DateOnly? to, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.GeoAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/devices", (string id, DateOnly? from, DateOnly? to, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.DevicesAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/events", (string id, DateOnly? from, DateOnly? to, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.EventsAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/realtime", (string id, StatsService svc, ISiteRepository sites, CancellationToken ct) =>
    WithSite(id, sites, ct, siteId => svc.RealtimeAsync(siteId, ct)));

static DateOnly From(DateOnly? from) => from ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-29);
static DateOnly To(DateOnly? to) => to ?? DateOnly.FromDateTime(DateTime.UtcNow);

static async Task<IResult> WithSite<T>(string id, ISiteRepository sites, CancellationToken ct, Func<SiteId, Task<T>> query)
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    if (await sites.GetAsync(siteId, ct) is null) return Results.NotFound();
    return Results.Ok(await query(siteId));
}

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
