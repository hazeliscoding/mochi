using Mochi.Domain.Goals;

namespace Mochi.Api.Contracts;

/// <summary>Body for POST /api/sites/{id}/goals.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Type">"page", "event", "outbound" or "download".</param>
/// <param name="Target">Path or event name to match.</param>
public sealed record GoalRequest(string? Name, string? Type, string? Target);

/// <summary>Goal as returned by the goals endpoints.</summary>
/// <param name="Id">Goal id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Type">Goal type in wire form.</param>
/// <param name="Target">Matched path or event name.</param>
public sealed record GoalResponse(string Id, string Name, string Type, string Target)
{
    /// <summary>Maps the aggregate to the wire shape.</summary>
    public static GoalResponse From(Goal goal)
        => new(goal.Id, goal.Name, goal.Type.ToString().ToLowerInvariant(), goal.Target);

    /// <summary>Parses the wire type value. Returns null for unknown input.</summary>
    public static GoalType? ParseType(string? wire) => wire switch
    {
        "page" => GoalType.Page,
        "event" => GoalType.Event,
        "outbound" => GoalType.Outbound,
        "download" => GoalType.Download,
        _ => null,
    };
}
