using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mochi.Api.Auth;
using Mochi.Api.Contracts;
using Mochi.Application.Abstractions;
using Mochi.Application.Auth;
using Mochi.Application.Collect;
using Mochi.Application.Privacy;
using Mochi.Application.Rollups;
using Mochi.Application.Sites;
using Mochi.Application.Stats;
using Mochi.Domain.Accounts;
using Mochi.Domain.Goals;
using Mochi.Domain.Sites;
using Mochi.Infrastructure;
using Mochi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMochi(builder.Configuration);

// Per-IP fixed windows (ADR 0002/0004 open questions, resolved for v1.0).
// The IP is used only as an in-memory partition key, consistent with how
// ingest already treats it: transient, never persisted. Limits are config
// overridable; tests use tight ones.
var authLimit = builder.Configuration.GetValue("Mochi:RateLimits:AuthPerMinute", 10);
var collectLimit = builder.Configuration.GetValue("Mochi:RateLimits:CollectPerMinute", 120);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = authLimit,
            Window = TimeSpan.FromMinutes(1),
        }));
    o.AddPolicy("collect", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            PermitLimit = collectLimit,
            Window = TimeSpan.FromMinutes(1),
        }));
});

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

// Print the first-run setup code while setup is still open (ADR 0004).
using (var scope = app.Services.CreateScope())
{
    if (await scope.ServiceProvider.GetRequiredService<AuthService>().NeedsSetupAsync())
    {
        app.Logger.LogInformation("No account exists yet. First-run setup code: {Code}",
            scope.ServiceProvider.GetRequiredService<ISetupCodeProvider>().Code);
    }
}

// Behind a TLS-terminating proxy (Railway, fly.io, nginx) the real client IP
// and scheme arrive in forwarded headers. Without this every visitor would
// share the proxy's IP, collapsing visitor hashes, and cookies would never be
// Secure. Opt-in because trusting these headers off-proxy allows spoofing.
if (app.Configuration.GetValue<bool>("Mochi:TrustProxyHeaders"))
{
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

app.UseRateLimiter();

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// Serves wwwroot/script.js, the embeddable tracking snippet. Cross-origin by
// nature (script tags are exempt from CORS); cached for a day so busy sites
// do not hammer the API, short enough that fixes roll out fast.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=86400",
});

// Session and CSRF checks for everything under /api except collect and auth.
app.UseMiddleware<SessionAuthMiddleware>();

app.MapAuth();

// Ingestion. Body is text/plain JSON so browsers skip the CORS preflight.
// Do not "fix" the content type; every analytics vendor does this (ADR 0002).
// Invalid-but-parseable payloads still get 202 so probes and blockers learn
// nothing; drop reasons go to the log only. Malformed JSON is the one 400.
// Never authenticated (ADR 0004).
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
}).RequireRateLimiting("collect");

// Site management. All endpoints below run behind the session middleware;
// membership decides which sites a user can even see (ADR 0004: anonymous
// gets 401 from the middleware, a non-member gets 404, never 403).
var snippetBaseUrl = app.Configuration["Mochi:SnippetBaseUrl"] ?? "http://localhost:5000";

app.MapPost("/api/sites", async (SiteRequest req, HttpContext ctx, RegisterSiteHandler handler, IMembershipRepository members, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Domain) || string.IsNullOrWhiteSpace(req.Timezone))
        return Results.UnprocessableEntity("name, domain and timezone are required");

    var site = await handler.HandleAsync(req.Name, req.Domain, req.Timezone, ct);
    await members.AddAsync(new SiteMembership(SessionAuthMiddleware.CurrentUser(ctx)!.Id, site.Id, SiteRole.Owner), ct);
    return Results.Created($"/api/sites/{site.Id.Value}", SiteResponse.From(site, snippetBaseUrl));
});

app.MapGet("/api/sites", async (HttpContext ctx, ISiteRepository sites, IMembershipRepository members, StatsService stats, CancellationToken ct) =>
{
    var mine = (await members.ListSiteIdsAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, ct)).ToHashSet();
    var items = new List<SiteListItem>();
    foreach (var s in (await sites.ListAsync(ct)).Where(s => mine.Contains(s.Id)))
    {
        var o = await stats.OverviewAsync(s.Id, ct);
        items.Add(new SiteListItem(SiteResponse.From(s, snippetBaseUrl), o.ViewsLast30d, o.ActiveNow, o.ViewsLast30d > 0 ? "active" : "waiting"));
    }

    return Results.Ok(items);
});

app.MapGet("/api/sites/{id}", (string id, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithMemberSite(ctx, id, sites, members, ct, site => Task.FromResult(SiteResponse.From(site, snippetBaseUrl))));

app.MapPut("/api/sites/{id}", (string id, SiteRequest req, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithMemberSite(ctx, id, sites, members, ct, async site =>
    {
        var retention = SiteResponse.ParseRetention(req.Retention) ?? site.Retention;
        site.UpdateSettings(req.Name ?? site.Name, req.Timezone ?? site.Timezone, retention);
        await sites.UpdateAsync(site, ct);
        return SiteResponse.From(site, snippetBaseUrl);
    }));

// Deleting a site deletes all its data immediately: raw events, rollups, then
// the site row. The Privacy Center promise depends on this (ADR 0002).
app.MapDelete("/api/sites/{id}", async (string id, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, IAnalyticsEventStore events, IRollupStore rollups, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    if (!await members.IsMemberAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, siteId, ct)) return Results.NotFound();
    await events.PurgeSiteAsync(siteId, ct);
    await rollups.PurgeSiteAsync(siteId, ct);
    await sites.RemoveAsync(siteId, ct);
    return Results.NoContent();
});

// Stats queries (ADR 0002). from/to are inclusive UTC days, defaulting to the
// last 30 days; compare is "previous", "year" or absent.
var stats = app.MapGroup("/api/sites/{id}/stats");

stats.MapGet("/summary", (string id, DateOnly? from, DateOnly? to, string? compare, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.SummaryAsync(siteId, From(from), To(to), compare, ct)));

stats.MapGet("/timeseries", (string id, DateOnly? from, DateOnly? to, string? metric, string? compare, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.TimeseriesAsync(siteId, From(from), To(to), metric ?? "visitors", compare, ct)));

stats.MapGet("/pages", (string id, DateOnly? from, DateOnly? to, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.PagesAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/sources", (string id, DateOnly? from, DateOnly? to, string? group, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.SourcesAsync(siteId, From(from), To(to), group ?? "channels", ct)));

stats.MapGet("/geo", (string id, DateOnly? from, DateOnly? to, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.GeoAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/devices", (string id, DateOnly? from, DateOnly? to, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.DevicesAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/events", (string id, DateOnly? from, DateOnly? to, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.EventsAsync(siteId, From(from), To(to), ct)));

stats.MapGet("/realtime", (string id, HttpContext ctx, StatsService svc, ISiteRepository sites, IMembershipRepository members, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, siteId => svc.RealtimeAsync(siteId, ct)));

static DateOnly From(DateOnly? from) => from ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-29);
static DateOnly To(DateOnly? to) => to ?? DateOnly.FromDateTime(DateTime.UtcNow);

// Parse, existence and membership in one place. Non-members get the same 404
// as nonexistent sites so short public ids leak nothing (ADR 0004).
static async Task<IResult> WithSite<T>(HttpContext ctx, string id, ISiteRepository sites, IMembershipRepository members, CancellationToken ct, Func<SiteId, Task<T>> query)
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    if (await sites.GetAsync(siteId, ct) is null) return Results.NotFound();
    if (!await members.IsMemberAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, siteId, ct)) return Results.NotFound();
    return Results.Ok(await query(siteId));
}

// Same checks, but hands the loaded aggregate to the callback.
static async Task<IResult> WithMemberSite<T>(HttpContext ctx, string id, ISiteRepository sites, IMembershipRepository members, CancellationToken ct, Func<Site, Task<T>> query)
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    var site = await sites.GetAsync(siteId, ct);
    if (site is null) return Results.NotFound();
    if (!await members.IsMemberAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, siteId, ct)) return Results.NotFound();
    return Results.Ok(await query(site));
}

// Goals (ADR 0002): CRUD plus conversion stats computed at query time, so a
// new goal shows history immediately.
app.MapPost("/api/sites/{id}/goals", async (string id, GoalRequest req, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, IGoalRepository goals, IClock clock, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId) || await sites.GetAsync(siteId, ct) is null) return Results.NotFound();
    if (!await members.IsMemberAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, siteId, ct)) return Results.NotFound();
    var type = GoalResponse.ParseType(req.Type);
    if (type is null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Target))
        return Results.UnprocessableEntity("name, type and target are required");

    var goal = Goal.Create(siteId, req.Name, type.Value, req.Target, clock.UtcNow);
    await goals.AddAsync(goal, ct);
    return Results.Created($"/api/sites/{id}/goals/{goal.Id}", GoalResponse.From(goal));
});

app.MapGet("/api/sites/{id}/goals", (string id, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, IGoalRepository goals, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, async siteId => (await goals.ListAsync(siteId, ct)).Select(GoalResponse.From)));

app.MapDelete("/api/sites/{id}/goals/{goalId}", async (string id, string goalId, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, IGoalRepository goals, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    if (!await members.IsMemberAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, siteId, ct)) return Results.NotFound();
    await goals.RemoveAsync(siteId, goalId, ct);
    return Results.NoContent();
});

app.MapGet("/api/sites/{id}/goals/stats", (string id, DateOnly? from, DateOnly? to, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, IGoalRepository goals, StatsService svc, CancellationToken ct) =>
    WithSite(ctx, id, sites, members, ct, async siteId =>
        await svc.GoalStatsAsync(siteId, await goals.ListAsync(siteId, ct), From(from), To(to), ct)));

// Privacy center (v0.6): live facts about what is held, and the full export.
app.MapGet("/api/sites/{id}/privacy", (string id, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, PrivacyService svc, CancellationToken ct) =>
    WithMemberSite(ctx, id, sites, members, ct, async site =>
    {
        var s = await svc.SummaryAsync(site, ct);
        return new
        {
            retention = SiteResponse.RetentionToWire(s.Retention),
            rawEventLifetimeDays = s.RawEventLifetimeDays,
            rawEventsHeld = s.RawEventsHeld,
            oldestAggregateDate = s.OldestAggregateDate?.ToString("yyyy-MM-dd"),
        };
    }));

// The export is a zip of CSVs, one per rollup table. Aggregates only; there
// is no per-visitor data to export (ADR 0001).
app.MapGet("/api/sites/{id}/export", async (string id, HttpContext ctx, ISiteRepository sites, IMembershipRepository members, ExportService svc, CancellationToken ct) =>
{
    if (!SiteId.TryParse(id, out var siteId)) return Results.NotFound();
    var site = await sites.GetAsync(siteId, ct);
    if (site is null) return Results.NotFound();
    if (!await members.IsMemberAsync(SessionAuthMiddleware.CurrentUser(ctx)!.Id, siteId, ct)) return Results.NotFound();

    var files = await svc.BuildAsync(site, ct);
    using var buffer = new MemoryStream();
    using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var (name, content) in files)
        {
            await using var stream = zip.CreateEntry(name).Open();
            await stream.WriteAsync(Encoding.UTF8.GetBytes(content), ct);
        }
    }

    return Results.File(buffer.ToArray(), "application/zip", $"mochi-export-{siteId.Value}.zip");
});

// Manual rollup rerun per ADR 0003. Session required by the middleware;
// additionally admin-only (ADR 0004). The endpoint's existence is public
// knowledge, so 403 is fine here.
app.MapPost("/api/admin/rollup/{date}", async (string date, HttpContext ctx, RollupJob job, CancellationToken ct) =>
{
    if (!SessionAuthMiddleware.CurrentUser(ctx)!.IsAdmin) return Results.Forbid();
    if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var day)) return Results.BadRequest("date must be yyyy-MM-dd");
    await job.RunForDayAsync(day, ct);
    return Results.Ok();
});

// In the self-hosted image the Angular bundle sits in wwwroot next to
// script.js and the API serves it. Unmatched non-API routes fall back to the
// SPA shell; unknown API routes stay 404. In development wwwroot has no
// index.html and ng serve owns the frontend, so this never triggers.
var spaIndex = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
if (File.Exists(spaIndex))
{
    app.MapFallback(async ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        ctx.Response.ContentType = "text/html";
        await ctx.Response.SendFileAsync(spaIndex);
    });
}

app.Run();

/// <summary>Marker so WebApplicationFactory can host the app in integration tests.</summary>
public partial class Program;
