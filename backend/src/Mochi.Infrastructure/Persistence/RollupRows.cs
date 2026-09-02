namespace Mochi.Infrastructure.Persistence;

// Row types for the daily_* tables (ADR 0003). Persistence shapes only, not
// domain types. Nullable dimensions (referrer domain, campaign) are stored as
// empty strings because they are part of composite primary keys.

internal sealed class SiteStatsRow
{
    public required string SiteId { get; init; }
    public required DateOnly Date { get; init; }
    public required int Visitors { get; init; }
    public required int Pageviews { get; init; }
    public required int Sessions { get; init; }
    public required int BouncedSessions { get; init; }
    public required long TotalSessionDurationSec { get; init; }
}

internal sealed class PageRow
{
    public required string SiteId { get; init; }
    public required DateOnly Date { get; init; }
    public required string Path { get; init; }
    public required int Visitors { get; init; }
    public required int Pageviews { get; init; }
    public required int Entries { get; init; }
    public required int Exits { get; init; }
    public required int BouncedSessions { get; init; }
    public required long TotalDurationSec { get; init; }
}

internal sealed class SourceRow
{
    public required string SiteId { get; init; }
    public required DateOnly Date { get; init; }
    public required short Channel { get; init; }
    public required string ReferrerDomain { get; init; }
    public required string Campaign { get; init; }
    public required int Visitors { get; init; }
    public required int Pageviews { get; init; }
    public required int BouncedSessions { get; init; }
}

internal sealed class GeoRow
{
    public required string SiteId { get; init; }
    public required DateOnly Date { get; init; }
    public required string Country { get; init; }
    public required int Visitors { get; init; }
}

internal sealed class DeviceRow
{
    public required string SiteId { get; init; }
    public required DateOnly Date { get; init; }
    public required short DeviceClass { get; init; }
    public required string Browser { get; init; }
    public required string Os { get; init; }
    public required int Visitors { get; init; }
}

internal sealed class EventRow
{
    public required string SiteId { get; init; }
    public required DateOnly Date { get; init; }
    public required string EventName { get; init; }
    public required string Path { get; init; }
    public required short Channel { get; init; }
    public required int Total { get; init; }
    public required int UniqueVisitors { get; init; }
}
