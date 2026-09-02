using Mochi.Domain.Goals;
using Mochi.Domain.Sites;

namespace Mochi.Application.Abstractions;

/// <summary>Persistence port for goals.</summary>
public interface IGoalRepository
{
    /// <summary>Returns the goal or null. The goal must belong to the site.</summary>
    Task<Goal?> GetAsync(SiteId siteId, string goalId, CancellationToken ct = default);

    /// <summary>All goals for a site, newest first.</summary>
    Task<IReadOnlyList<Goal>> ListAsync(SiteId siteId, CancellationToken ct = default);

    /// <summary>Adds a goal.</summary>
    Task AddAsync(Goal goal, CancellationToken ct = default);

    /// <summary>Deletes a goal. No-op when it does not exist.</summary>
    Task RemoveAsync(SiteId siteId, string goalId, CancellationToken ct = default);
}
