using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Application.Rollups;

/// <summary>
/// Turns one site's raw events for one closed day into rollup rows (ADR 0003).
/// Sessions are same-day event runs per visitor hash, split on gaps over
/// 30 minutes. The same logic must be mirrored by same-day dashboard queries
/// so "today" and "yesterday" agree in definition.
/// </summary>
public static class Sessionizer
{
    /// <summary>Maximum gap between events inside one session.</summary>
    public static readonly TimeSpan SessionGap = TimeSpan.FromMinutes(30);

    private sealed record Session(VisitorHash Visitor, IReadOnlyList<AnalyticsEvent> Events)
    {
        public IEnumerable<AnalyticsEvent> Pageviews => Events.Where(e => e.Type == EventType.Pageview);
        public int PageviewCount => Pageviews.Count();
        public bool Bounced => PageviewCount == 1;
        public long DurationSec => (long)(Events[^1].OccurredAt - Events[0].OccurredAt).TotalSeconds;
        public AnalyticsEvent First => Events[0];
    }

    /// <summary>Computes the full rollup batch for one site and one day.</summary>
    /// <param name="siteId">Site the events belong to.</param>
    /// <param name="date">The closed UTC day being rolled up.</param>
    /// <param name="dayEvents">All of that site's events for that day, any order.</param>
    public static RollupBatch Roll(SiteId siteId, DateOnly date, IReadOnlyCollection<AnalyticsEvent> dayEvents)
    {
        var sessions = Split(dayEvents);
        var pageviews = dayEvents.Where(e => e.Type == EventType.Pageview).ToList();
        var visitors = dayEvents.Select(e => e.Visitor).Distinct().Count();

        var stats = new DailySiteStats(
            visitors,
            pageviews.Count,
            sessions.Count,
            sessions.Count(s => s.Bounced),
            sessions.Sum(s => s.DurationSec));

        return new RollupBatch(siteId, date, stats,
            RollPages(sessions, pageviews),
            RollSources(sessions),
            RollGeo(dayEvents),
            RollDevices(dayEvents),
            RollEvents(dayEvents));
    }

    private static List<Session> Split(IReadOnlyCollection<AnalyticsEvent> events)
    {
        var sessions = new List<Session>();
        foreach (var run in events.GroupBy(e => e.Visitor))
        {
            var ordered = run.OrderBy(e => e.OccurredAt).ToList();
            var start = 0;
            for (var i = 1; i <= ordered.Count; i++)
            {
                if (i == ordered.Count || ordered[i].OccurredAt - ordered[i - 1].OccurredAt > SessionGap)
                {
                    sessions.Add(new Session(run.Key, ordered[start..i]));
                    start = i;
                }
            }
        }

        return sessions;
    }

    private static List<DailyPageRow> RollPages(List<Session> sessions, List<AnalyticsEvent> pageviews)
    {
        // Time on a page is the gap to the next event in the same session; the
        // session's last page gets zero. Consistent with duration = last - first.
        var timeOnPath = new Dictionary<string, long>();
        var entries = new Dictionary<string, int>();
        var exits = new Dictionary<string, int>();
        var bounces = new Dictionary<string, int>();

        foreach (var s in sessions)
        {
            var pvs = s.Pageviews.ToList();
            if (pvs.Count == 0) continue;

            entries[pvs[0].Path] = entries.GetValueOrDefault(pvs[0].Path) + 1;
            exits[pvs[^1].Path] = exits.GetValueOrDefault(pvs[^1].Path) + 1;
            if (s.Bounced) bounces[pvs[0].Path] = bounces.GetValueOrDefault(pvs[0].Path) + 1;

            for (var i = 0; i < s.Events.Count - 1; i++)
            {
                if (s.Events[i].Type != EventType.Pageview) continue;
                var gap = (long)(s.Events[i + 1].OccurredAt - s.Events[i].OccurredAt).TotalSeconds;
                timeOnPath[s.Events[i].Path] = timeOnPath.GetValueOrDefault(s.Events[i].Path) + gap;
            }
        }

        return pageviews
            .GroupBy(e => e.Path)
            .Select(g => new DailyPageRow(
                g.Key,
                g.Select(e => e.Visitor).Distinct().Count(),
                g.Count(),
                entries.GetValueOrDefault(g.Key),
                exits.GetValueOrDefault(g.Key),
                bounces.GetValueOrDefault(g.Key),
                timeOnPath.GetValueOrDefault(g.Key)))
            .OrderByDescending(r => r.Pageviews)
            .ToList();
    }

    private static List<DailySourceRow> RollSources(List<Session> sessions)
    {
        // A session is attributed to the source of its first event.
        return sessions
            .GroupBy(s => (s.First.Channel, s.First.ReferrerDomain, s.First.Campaign))
            .Select(g => new DailySourceRow(
                g.Key.Channel,
                g.Key.ReferrerDomain,
                g.Key.Campaign,
                g.Select(s => s.Visitor).Distinct().Count(),
                g.Sum(s => s.PageviewCount),
                g.Count(s => s.Bounced)))
            .OrderByDescending(r => r.Visitors)
            .ToList();
    }

    private static List<DailyGeoRow> RollGeo(IReadOnlyCollection<AnalyticsEvent> events)
        => events
            .Where(e => e.CountryCode is not null)
            .GroupBy(e => e.CountryCode!)
            .Select(g => new DailyGeoRow(g.Key, g.Select(e => e.Visitor).Distinct().Count()))
            .OrderByDescending(r => r.Visitors)
            .ToList();

    private static List<DailyDeviceRow> RollDevices(IReadOnlyCollection<AnalyticsEvent> events)
        => events
            .GroupBy(e => (e.DeviceClass, e.Browser, e.Os))
            .Select(g => new DailyDeviceRow(g.Key.DeviceClass, g.Key.Browser, g.Key.Os, g.Select(e => e.Visitor).Distinct().Count()))
            .OrderByDescending(r => r.Visitors)
            .ToList();

    private static List<DailyEventRow> RollEvents(IReadOnlyCollection<AnalyticsEvent> events)
        => events
            .Where(e => e.Type == EventType.Custom)
            .GroupBy(e => (Name: e.EventName!, e.Path, e.Channel))
            .Select(g => new DailyEventRow(g.Key.Name, g.Key.Path, g.Key.Channel, g.Count(), g.Select(e => e.Visitor).Distinct().Count()))
            .OrderByDescending(r => r.Total)
            .ToList();
}
