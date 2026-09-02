using Mochi.Domain.Sites;

namespace Mochi.Domain.Goals;

/// <summary>
/// A user-defined conversion goal. Goals are filters over existing rollups,
/// so a new goal shows historical data immediately (ADR 0003).
/// </summary>
public sealed class Goal
{
    private Goal(string id, SiteId siteId, string name, GoalType type, string target, DateTimeOffset createdAt)
    {
        Id = id;
        SiteId = siteId;
        Name = name;
        Type = type;
        Target = target;
        CreatedAt = createdAt;
    }

    /// <summary>Opaque goal id, prefixed "g_".</summary>
    public string Id { get; }

    /// <summary>Site the goal belongs to.</summary>
    public SiteId SiteId { get; }

    /// <summary>Display name shown in the dashboard.</summary>
    public string Name { get; private set; }

    /// <summary>What the goal matches against.</summary>
    public GoalType Type { get; }

    /// <summary>The path or event name to match, depending on <see cref="Type"/>.</summary>
    public string Target { get; }

    /// <summary>When the goal was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Creates a goal for a site.</summary>
    /// <exception cref="ArgumentException">Name or target is empty.</exception>
    public static Goal Create(SiteId siteId, string name, GoalType type, string target, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        return new Goal($"g_{Guid.NewGuid():N}", siteId, name.Trim(), type, target.Trim(), now);
    }
}
