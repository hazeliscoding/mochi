using Mochi.Application.Rollups;
using Mochi.Domain.Sites;

namespace Mochi.Application.Abstractions;

/// <summary>Store for the daily_* rollup tables (ADR 0003).</summary>
public interface IRollupStore
{
    /// <summary>
    /// Replaces one site's rollup rows for one day with the batch. Delete and
    /// rewrite, so reruns are idempotent.
    /// </summary>
    Task ReplaceDayAsync(RollupBatch batch, CancellationToken ct = default);

    /// <summary>Deletes one site's rollup rows older than <paramref name="cutoff"/>.</summary>
    Task PurgeBeforeAsync(SiteId siteId, DateOnly cutoff, CancellationToken ct = default);

    /// <summary>Deletes all rollup rows for one site. Used by site deletion.</summary>
    Task PurgeSiteAsync(SiteId siteId, CancellationToken ct = default);
}
