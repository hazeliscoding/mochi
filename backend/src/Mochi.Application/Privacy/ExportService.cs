using System.Globalization;
using System.Text;
using Mochi.Application.Abstractions;
using Mochi.Domain.Goals;
using Mochi.Domain.Sites;

namespace Mochi.Application.Privacy;

/// <summary>
/// Builds a site's full data export: one CSV per rollup table plus goals.
/// This is everything Mochi holds long-term; raw events are excluded because
/// they expire in days and exist only to be rolled up (ADR 0003).
/// </summary>
public sealed class ExportService(IRollupReader rollups, IGoalRepository goals, IClock clock)
{
    /// <summary>File name to file content for the export archive.</summary>
    public async Task<IReadOnlyDictionary<string, string>> BuildAsync(Site site, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var from = await rollups.OldestDateAsync(site.Id, ct) ?? today;

        var files = new Dictionary<string, string>
        {
            ["readme.txt"] =
                $"Mochi data export for {site.Domain} ({site.Id.Value}), generated {clock.UtcNow:yyyy-MM-dd HH:mm} UTC.\n" +
                "Contains every daily aggregate Mochi stores. There is no per-visitor data to export;\n" +
                "visitors are day-scoped hashes and raw events expire after 7 days.\n" +
                "Today's traffic appears after the nightly rollup.\n",
            ["daily_site_stats.csv"] = Csv(
                ["date", "visitors", "pageviews", "sessions", "bounced_sessions", "total_session_duration_sec"],
                (await rollups.SiteStatsAsync(site.Id, from, today, ct))
                    .Select(d => Row(d.Date, d.Stats.Visitors, d.Stats.Pageviews, d.Stats.Sessions, d.Stats.BouncedSessions, d.Stats.TotalSessionDurationSec))),
            ["daily_pages.csv"] = Csv(
                ["date", "path", "visitors", "pageviews", "entries", "exits", "bounced_sessions", "total_duration_sec"],
                (await rollups.PagesAsync(site.Id, from, today, ct))
                    .Select(d => Row(d.Date, d.Row.Path, d.Row.Visitors, d.Row.Pageviews, d.Row.Entries, d.Row.Exits, d.Row.BouncedSessions, d.Row.TotalDurationSec))),
            ["daily_sources.csv"] = Csv(
                ["date", "channel", "referrer_domain", "campaign", "visitors", "pageviews", "bounced_sessions"],
                (await rollups.SourcesAsync(site.Id, from, today, ct))
                    .Select(d => Row(d.Date, d.Row.Channel, d.Row.ReferrerDomain, d.Row.Campaign, d.Row.Visitors, d.Row.Pageviews, d.Row.BouncedSessions))),
            ["daily_geo.csv"] = Csv(
                ["date", "country", "visitors"],
                (await rollups.GeoAsync(site.Id, from, today, ct))
                    .Select(d => Row(d.Date, d.Row.Country, d.Row.Visitors))),
            ["daily_devices.csv"] = Csv(
                ["date", "device_class", "browser", "os", "visitors"],
                (await rollups.DevicesAsync(site.Id, from, today, ct))
                    .Select(d => Row(d.Date, d.Row.Device, d.Row.Browser, d.Row.Os, d.Row.Visitors))),
            ["daily_events.csv"] = Csv(
                ["date", "event_name", "path", "channel", "total", "unique_visitors"],
                (await rollups.EventsAsync(site.Id, from, today, ct))
                    .Select(d => Row(d.Date, d.Row.EventName, d.Row.Path, d.Row.Channel, d.Row.Total, d.Row.UniqueVisitors))),
            ["goals.csv"] = Csv(
                ["id", "name", "type", "target", "created_at"],
                (await goals.ListAsync(site.Id, ct))
                    .Select(g => Row(g.Id, g.Name, g.Type, g.Target, g.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)))),
        };

        return files;
    }

    private static string Row(params object?[] values)
        => string.Join(',', values.Select(Field));

    private static string Field(object? value)
    {
        var s = value switch
        {
            null => string.Empty,
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            GoalType t => t.ToString().ToLowerInvariant(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
        return s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    private static string Csv(string[] header, IEnumerable<string> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', header));
        foreach (var row in rows) sb.AppendLine(row);
        return sb.ToString();
    }
}
