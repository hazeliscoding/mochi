using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;
using Mochi.Domain.Sites;

namespace Mochi.Application.Privacy;

/// <summary>
/// Live facts for one site's privacy summary. Every number is queried, not
/// copy: the page renders what the system actually holds.
/// </summary>
/// <param name="Retention">The site's retention policy.</param>
/// <param name="RawEventLifetimeDays">Fixed raw-event window (ADR 0003).</param>
/// <param name="RawEventsHeld">Scrubbed raw events currently stored.</param>
/// <param name="OldestAggregateDate">Oldest daily aggregate, or null before the first rollup.</param>
public sealed record PrivacySummary(RetentionPolicy Retention, int RawEventLifetimeDays, long RawEventsHeld, DateOnly? OldestAggregateDate);

/// <summary>Builds the privacy summary from the stores.</summary>
public sealed class PrivacyService(IRollupReader rollups, IAnalyticsEventStore events)
{
    /// <summary>Queries the live numbers for one site.</summary>
    public async Task<PrivacySummary> SummaryAsync(Site site, CancellationToken ct = default)
        => new(
            site.Retention,
            (int)RollupJob.RawEventLifetime.TotalDays,
            await events.CountAsync(site.Id, ct),
            await rollups.OldestDateAsync(site.Id, ct));
}
