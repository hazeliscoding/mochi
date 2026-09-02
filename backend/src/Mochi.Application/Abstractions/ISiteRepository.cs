using Mochi.Domain.Sites;

namespace Mochi.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Site"/> aggregate.</summary>
public interface ISiteRepository
{
    /// <summary>Returns the site or null if the id is unknown.</summary>
    Task<Site?> GetAsync(SiteId id, CancellationToken ct = default);

    /// <summary>Returns all sites, newest first.</summary>
    Task<IReadOnlyList<Site>> ListAsync(CancellationToken ct = default);

    /// <summary>Adds a new site.</summary>
    Task AddAsync(Site site, CancellationToken ct = default);

    /// <summary>Persists changed settings of an existing site.</summary>
    Task UpdateAsync(Site site, CancellationToken ct = default);

    /// <summary>Deletes the site. Event and rollup data must cascade (ADR 0003).</summary>
    Task RemoveAsync(SiteId id, CancellationToken ct = default);
}
