using Mochi.Application.Abstractions;
using Mochi.Domain.Sites;

namespace Mochi.Application.Rollups;

/// <summary>
/// The nightly rollup and purge (ADR 0003). Idempotent per site and day:
/// rerunning replaces the day's rows. Salt rotation is not done here; the
/// salt provider rotates lazily at the UTC day boundary.
/// </summary>
public sealed class RollupJob(ISiteRepository sites, IAnalyticsEventStore events, IRollupStore rollups, IClock clock)
{
    /// <summary>Raw events are kept this long, then purged. A processing buffer, not a retention feature.</summary>
    public static readonly TimeSpan RawEventLifetime = TimeSpan.FromDays(7);

    /// <summary>Rolls up one UTC day for every site, then runs the purges.</summary>
    public async Task RunForDayAsync(DateOnly utcDay, CancellationToken ct = default)
    {
        foreach (var site in await sites.ListAsync(ct))
        {
            await RunForSiteAsync(site, utcDay, ct);
        }

        await events.PurgeBeforeAsync(clock.UtcNow - RawEventLifetime, ct);
    }

    /// <summary>Rolls up one UTC day for one site and applies its retention purge.</summary>
    public async Task RunForSiteAsync(Site site, DateOnly utcDay, CancellationToken ct = default)
    {
        var dayEvents = await events.ReadDayAsync(site.Id, utcDay, ct);
        await rollups.ReplaceDayAsync(Sessionizer.Roll(site.Id, utcDay, dayEvents), ct);

        var cutoff = RetentionCutoff(site.Retention, utcDay);
        if (cutoff is not null)
        {
            await rollups.PurgeBeforeAsync(site.Id, cutoff.Value, ct);
        }
    }

    /// <summary>Oldest rollup date a site keeps, or null for unlimited retention.</summary>
    private static DateOnly? RetentionCutoff(RetentionPolicy retention, DateOnly today) => retention switch
    {
        RetentionPolicy.Days30 => today.AddDays(-30),
        RetentionPolicy.Days90 => today.AddDays(-90),
        RetentionPolicy.OneYear => today.AddYears(-1),
        _ => null,
    };
}
