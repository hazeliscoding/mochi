using System.Globalization;
using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Domain.Collection;
using Mochi.Domain.Goals;
using Mochi.Domain.Sites;

namespace Mochi.Application.Stats;

/// <summary>
/// Serves the dashboard queries. Closed days read from rollups; when the range
/// includes today, today's raw events are sessionized at query time with the
/// same logic the nightly job uses, so definitions agree (ADR 0003).
/// Days are UTC (known day-bucket tradeoff, ADR 0003 open questions).
/// </summary>
public sealed class StatsService(IRollupReader rollups, IAnalyticsEventStore events, IClock clock)
{
    private DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    /// <summary>Summary metrics with optional comparison. Compare is null when the compare range has no data.</summary>
    public async Task<SummaryResponse> SummaryAsync(SiteId siteId, DateOnly from, DateOnly to, string? compare, CancellationToken ct = default)
    {
        var current = await SummaryForRangeAsync(siteId, from, to, ct);
        SummaryStats? cmp = null;
        if (CompareRange(from, to, compare) is var (cf, ctto) && compare is "previous" or "year")
        {
            var c = await SummaryForRangeAsync(siteId, cf, ctto, ct);
            if (c.Visitors > 0 || c.Pageviews > 0) cmp = c;
        }

        return new SummaryResponse(current, cmp);
    }

    /// <summary>Daily series for visitors, pageviews or sessions.</summary>
    public async Task<TimeseriesResponse> TimeseriesAsync(SiteId siteId, DateOnly from, DateOnly to, string metric, string? compare, CancellationToken ct = default)
    {
        var points = await SeriesAsync(siteId, from, to, metric, ct);
        IReadOnlyList<MetricPoint>? cmp = null;
        if (compare is "previous" or "year")
        {
            var (cf, ctto) = CompareRange(from, to, compare);
            var c = await SeriesAsync(siteId, cf, ctto, metric, ct);
            if (c.Any(p => p.Value > 0)) cmp = c;
        }

        return new TimeseriesResponse(points, cmp);
    }

    /// <summary>Pages table rows, most viewed first.</summary>
    public async Task<IReadOnlyList<PageStatsRow>> PagesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = new List<DailyPageRow>((await rollups.PagesAsync(siteId, from, to, ct)).Select(d => d.Row));
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) rows.AddRange(today.Pages);

        return rows
            .GroupBy(r => r.Path)
            .Select(g =>
            {
                var entries = g.Sum(r => r.Entries);
                var pageviews = g.Sum(r => r.Pageviews);
                return new PageStatsRow(
                    g.Key,
                    g.Sum(r => r.Visitors),
                    pageviews,
                    Pct(g.Sum(r => r.BouncedSessions), entries),
                    pageviews == 0 ? 0 : Math.Round((double)g.Sum(r => r.TotalDurationSec) / pageviews),
                    entries,
                    g.Sum(r => r.Exits));
            })
            .OrderByDescending(r => r.Pageviews)
            .ToList();
    }

    /// <summary>Source rows for one tab: channels, referrers, search, social or campaigns.</summary>
    public async Task<IReadOnlyList<CountRow>> SourcesAsync(SiteId siteId, DateOnly from, DateOnly to, string group, CancellationToken ct = default)
    {
        var rows = new List<DailySourceRow>((await rollups.SourcesAsync(siteId, from, to, ct)).Select(d => d.Row));
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) rows.AddRange(today.Sources);

        IEnumerable<(string Name, int Visitors)> grouped = group switch
        {
            "channels" => rows.GroupBy(r => r.Channel).Select(g => (g.Key.ToString(), g.Sum(r => r.Visitors))),
            "search" => ByDomain(rows.Where(r => r.Channel == Channel.Search)),
            "social" => ByDomain(rows.Where(r => r.Channel == Channel.Social)),
            "campaigns" => rows.Where(r => !string.IsNullOrEmpty(r.Campaign))
                .GroupBy(r => r.Campaign!).Select(g => (g.Key, g.Sum(r => r.Visitors))),
            _ => ByDomain(rows),
        };

        var list = grouped.OrderByDescending(x => x.Visitors).ToList();
        var total = list.Sum(x => x.Visitors);
        return [.. list.Select(x => new CountRow(x.Name, x.Visitors, Pct(x.Visitors, total)))];

        static IEnumerable<(string, int)> ByDomain(IEnumerable<DailySourceRow> src) => src
            .Where(r => !string.IsNullOrEmpty(r.ReferrerDomain))
            .GroupBy(r => r.ReferrerDomain!)
            .Select(g => (g.Key, g.Sum(r => r.Visitors)));
    }

    /// <summary>Country rows, most visitors first.</summary>
    public async Task<IReadOnlyList<GeoStatsRow>> GeoAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = new List<DailyGeoRow>((await rollups.GeoAsync(siteId, from, to, ct)).Select(d => d.Row));
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) rows.AddRange(today.Geo);

        var grouped = rows.GroupBy(r => r.Country).Select(g => (Code: g.Key, Visitors: g.Sum(r => r.Visitors)))
            .OrderByDescending(x => x.Visitors).ToList();
        var total = grouped.Sum(x => x.Visitors);
        return [.. grouped.Select(x => new GeoStatsRow(x.Code, CountryName(x.Code), x.Visitors, Pct(x.Visitors, total)))];
    }

    /// <summary>Device class, browser and OS breakdowns.</summary>
    public async Task<DevicesResponse> DevicesAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = new List<DailyDeviceRow>((await rollups.DevicesAsync(siteId, from, to, ct)).Select(d => d.Row));
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) rows.AddRange(today.Devices);

        return new DevicesResponse(
            Top(rows.GroupBy(r => r.Device.ToString())),
            Top(rows.GroupBy(r => r.Browser)),
            Top(rows.GroupBy(r => r.Os)));

        static IReadOnlyList<CountRow> Top(IEnumerable<IGrouping<string, DailyDeviceRow>> groups)
        {
            var list = groups.Select(g => (g.Key, Visitors: g.Sum(r => r.Visitors)))
                .OrderByDescending(x => x.Visitors).ToList();
            var total = list.Sum(x => x.Visitors);
            return [.. list.Select(x => new CountRow(x.Key, x.Visitors, Pct(x.Visitors, total)))];
        }
    }

    /// <summary>Custom events with per-page and per-channel breakdowns.</summary>
    public async Task<IReadOnlyList<EventStatsRow>> EventsAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rows = new List<DailyEventRow>((await rollups.EventsAsync(siteId, from, to, ct)).Select(d => d.Row));
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) rows.AddRange(today.Events);
        var totalVisitors = (await SummaryForRangeAsync(siteId, from, to, ct)).Visitors;

        return [.. rows
            .GroupBy(r => r.EventName)
            .Select(g =>
            {
                var total = g.Sum(r => r.Total);
                var uniq = g.Sum(r => r.UniqueVisitors);
                return new EventStatsRow(
                    g.Key,
                    total,
                    uniq,
                    Pct(uniq, totalVisitors),
                    Shares(g.GroupBy(r => r.Path).Select(p => (p.Key, p.Sum(r => r.Total))), total),
                    Shares(g.GroupBy(r => r.Channel.ToString()).Select(c => (c.Key, c.Sum(r => r.Total))), total));
            })
            .OrderByDescending(r => r.Total)];

        static IReadOnlyList<CountRow> Shares(IEnumerable<(string Name, int Count)> parts, int total)
            => [.. parts.OrderByDescending(p => p.Count).Select(p => new CountRow(p.Name, p.Count, Pct(p.Count, total)))];
    }

    /// <summary>Realtime view: 5-minute activity plus a 30-minute per-minute chart.</summary>
    public async Task<RealtimeResponse> RealtimeAsync(SiteId siteId, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var last30 = await events.ReadRecentAsync(siteId, now.AddMinutes(-30), ct);
        var last5 = last30.Where(e => e.OccurredAt >= now.AddMinutes(-5)).ToList();

        var perMinute = new int[30];
        foreach (var e in last30.Where(e => e.Type == EventType.Pageview))
        {
            var bucket = 29 - (int)(now - e.OccurredAt).TotalMinutes;
            if (bucket is >= 0 and < 30) perMinute[bucket]++;
        }

        return new RealtimeResponse(
            last5.Select(e => e.Visitor).Distinct().Count(),
            perMinute,
            Counts(last5.Where(e => e.Type == EventType.Pageview).Select(e => e.Path)),
            Counts(last5.Select(e => e.ReferrerDomain ?? e.Channel.ToString())),
            Counts(last5.Where(e => e.CountryCode is not null).Select(e => CountryName(e.CountryCode!))),
            new RealtimeDevices(
                DistinctVisitors(last5, DeviceClass.Desktop),
                DistinctVisitors(last5, DeviceClass.Mobile),
                DistinctVisitors(last5, DeviceClass.Tablet)));

        static IReadOnlyList<CountRow> Counts(IEnumerable<string> names)
        {
            var list = names.GroupBy(n => n).Select(g => (g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count).ToList();
            var total = list.Sum(x => x.Count);
            return [.. list.Select(x => new CountRow(x.Key, x.Count, Pct(x.Count, total)))];
        }

        static int DistinctVisitors(IEnumerable<AnalyticsEvent> evts, DeviceClass device)
            => evts.Where(e => e.DeviceClass == device).Select(e => e.Visitor).Distinct().Count();
    }

    /// <summary>
    /// Goal conversions computed at query time by matching goal targets
    /// against the page and event rollups (ADR 0003). New goals therefore
    /// show history immediately.
    /// </summary>
    public async Task<IReadOnlyList<GoalStatsRow>> GoalStatsAsync(SiteId siteId, IReadOnlyList<Goal> goals, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (goals.Count == 0) return [];

        var pages = new List<DailyPageRow>((await rollups.PagesAsync(siteId, from, to, ct)).Select(d => d.Row));
        var eventRows = new List<DailyEventRow>((await rollups.EventsAsync(siteId, from, to, ct)).Select(d => d.Row));
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today)
        {
            pages.AddRange(today.Pages);
            eventRows.AddRange(today.Events);
        }

        var totalVisitors = (await SummaryForRangeAsync(siteId, from, to, ct)).Visitors;

        return [.. goals.Select(g =>
        {
            var conversions = g.Type == GoalType.Page
                ? pages.Where(p => p.Path == g.Target).Sum(p => p.Visitors)
                : eventRows.Where(e => e.EventName == g.Target).Sum(e => e.UniqueVisitors);
            return new GoalStatsRow(g.Id, g.Name, g.Type.ToString().ToLowerInvariant(), g.Target, conversions, Pct(conversions, totalVisitors));
        })];
    }

    /// <summary>Live numbers for one site in the Websites list.</summary>
    public async Task<SiteOverview> OverviewAsync(SiteId siteId, CancellationToken ct = default)
    {
        var summary = await SummaryForRangeAsync(siteId, Today.AddDays(-29), Today, ct);
        var recent = await events.ReadRecentAsync(siteId, clock.UtcNow.AddMinutes(-5), ct);
        return new SiteOverview(summary.Pageviews, recent.Select(e => e.Visitor).Distinct().Count());
    }

    private async Task<SummaryStats> SummaryForRangeAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var days = (await rollups.SiteStatsAsync(siteId, from, to, ct)).Select(d => d.Stats).ToList();
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) days.Add(today.SiteStats);

        var visitors = days.Sum(d => d.Visitors);
        var pageviews = days.Sum(d => d.Pageviews);
        var sessions = days.Sum(d => d.Sessions);
        return new SummaryStats(
            visitors,
            pageviews,
            visitors == 0 ? 0 : Math.Round((double)pageviews / visitors, 2),
            Pct(days.Sum(d => d.BouncedSessions), sessions),
            sessions == 0 ? 0 : Math.Round((double)days.Sum(d => d.TotalSessionDurationSec) / sessions));
    }

    private async Task<IReadOnlyList<MetricPoint>> SeriesAsync(SiteId siteId, DateOnly from, DateOnly to, string metric, CancellationToken ct)
    {
        var byDate = (await rollups.SiteStatsAsync(siteId, from, to, ct)).ToDictionary(d => d.Date, d => d.Stats);
        if (await TodayBatchAsync(siteId, from, to, ct) is { } today) byDate[Today] = today.SiteStats;

        var points = new List<MetricPoint>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var s = byDate.GetValueOrDefault(d);
            long value = metric switch
            {
                "pageviews" => s?.Pageviews ?? 0,
                "sessions" => s?.Sessions ?? 0,
                _ => s?.Visitors ?? 0,
            };
            points.Add(new MetricPoint(d, value));
        }

        return points;
    }

    /// <summary>Sessionizes today's raw events when today falls inside the range, else null.</summary>
    private async Task<RollupBatch?> TodayBatchAsync(SiteId siteId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (Today < from || Today > to) return null;
        var dayEvents = await events.ReadDayAsync(siteId, Today, ct);
        return dayEvents.Count == 0 ? null : Sessionizer.Roll(siteId, Today, dayEvents);
    }

    private static (DateOnly From, DateOnly To) CompareRange(DateOnly from, DateOnly to, string? compare)
    {
        if (compare == "year") return (from.AddYears(-1), to.AddYears(-1));
        var length = to.DayNumber - from.DayNumber + 1;
        return (from.AddDays(-length), to.AddDays(-length));
    }

    private static double Pct(long part, long total) => total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);

    private static string CountryName(string code)
    {
        try
        {
            return new RegionInfo(code).EnglishName;
        }
        catch (ArgumentException)
        {
            return code;
        }
    }
}

