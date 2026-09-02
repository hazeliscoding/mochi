using System.Collections.Concurrent;
using Mochi.Application.Abstractions;
using Mochi.Domain.Goals;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.InMemory;

/// <summary>In-memory goal repository for development and tests.</summary>
public sealed class InMemoryGoalRepository : IGoalRepository
{
    private readonly ConcurrentDictionary<string, Goal> _goals = new();

    /// <inheritdoc />
    public Task<Goal?> GetAsync(SiteId siteId, string goalId, CancellationToken ct = default)
    {
        var goal = _goals.GetValueOrDefault(goalId);
        return Task.FromResult(goal?.SiteId == siteId ? goal : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Goal>> ListAsync(SiteId siteId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Goal>>(
            [.. _goals.Values.Where(g => g.SiteId == siteId).OrderByDescending(g => g.CreatedAt)]);

    /// <inheritdoc />
    public Task AddAsync(Goal goal, CancellationToken ct = default)
    {
        _goals[goal.Id] = goal;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(SiteId siteId, string goalId, CancellationToken ct = default)
    {
        if (_goals.TryGetValue(goalId, out var g) && g.SiteId == siteId) _goals.TryRemove(goalId, out _);
        return Task.CompletedTask;
    }
}
