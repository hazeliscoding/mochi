namespace Mochi.Application.Stats;

// Wire shapes for the stats endpoints (ADR 0002). Numbers only; the frontend
// service formats them for display.

/// <summary>One point of a daily time series.</summary>
/// <param name="Date">UTC day.</param>
/// <param name="Value">Metric value for that day.</param>
public sealed record MetricPoint(DateOnly Date, long Value);

/// <summary>Aggregate metrics for a date range.</summary>
/// <param name="Visitors">Sum of daily distinct visitors. Overcounts across days by design (ADR 0001).</param>
/// <param name="Pageviews">Total pageviews.</param>
/// <param name="ViewsPerVisitor">Pageviews divided by visitors.</param>
/// <param name="BounceRatePct">Bounced sessions as a percentage of sessions, 0 to 100.</param>
/// <param name="AvgDurationSec">Average session duration in seconds.</param>
public sealed record SummaryStats(int Visitors, int Pageviews, double ViewsPerVisitor, double BounceRatePct, double AvgDurationSec);

/// <summary>Summary plus the comparison period, when requested and available.</summary>
public sealed record SummaryResponse(SummaryStats Current, SummaryStats? Compare);

/// <summary>Daily series for one metric plus the comparison series.</summary>
public sealed record TimeseriesResponse(IReadOnlyList<MetricPoint> Points, IReadOnlyList<MetricPoint>? Compare);

/// <summary>One row of the pages table.</summary>
/// <param name="Path">Page path.</param>
/// <param name="Visitors">Distinct visitors, summed over days.</param>
/// <param name="Pageviews">Total pageviews.</param>
/// <param name="BouncePct">Bounced sessions entering here as a percentage of entries.</param>
/// <param name="AvgDurationSec">Average time on page in seconds.</param>
/// <param name="Entries">Sessions that started here.</param>
/// <param name="Exits">Sessions that ended here.</param>
public sealed record PageStatsRow(string Path, int Visitors, int Pageviews, double BouncePct, double AvgDurationSec, int Entries, int Exits);

/// <summary>Generic named count with its share of the group total.</summary>
public sealed record CountRow(string Name, int Count, double Pct);

/// <summary>One country row.</summary>
public sealed record GeoStatsRow(string Code, string Name, int Visitors, double Pct);

/// <summary>Device breakdowns for the devices page.</summary>
public sealed record DevicesResponse(IReadOnlyList<CountRow> Classes, IReadOnlyList<CountRow> Browsers, IReadOnlyList<CountRow> Os);

/// <summary>One custom event with its breakdowns.</summary>
/// <param name="Name">Event name.</param>
/// <param name="Total">Total occurrences.</param>
/// <param name="UniqueVisitors">Distinct visitors that fired it, summed over days.</param>
/// <param name="ConvPct">Unique visitors firing the event as a percentage of all visitors.</param>
/// <param name="Pages">Occurrences per page.</param>
/// <param name="Sources">Occurrences per channel.</param>
public sealed record EventStatsRow(string Name, int Total, int UniqueVisitors, double ConvPct, IReadOnlyList<CountRow> Pages, IReadOnlyList<CountRow> Sources);

/// <summary>Realtime view: last 5 minutes, plus a 30-minute pageview chart.</summary>
public sealed record RealtimeResponse(
    int ActiveVisitors,
    IReadOnlyList<int> PageviewsPerMinute,
    IReadOnlyList<CountRow> Pages,
    IReadOnlyList<CountRow> Sources,
    IReadOnlyList<CountRow> Countries,
    RealtimeDevices Devices);

/// <summary>Active visitor counts per device class.</summary>
public sealed record RealtimeDevices(int Desktop, int Mobile, int Tablet);

/// <summary>Live numbers for the Websites list.</summary>
public sealed record SiteOverview(long ViewsLast30d, int ActiveNow);
