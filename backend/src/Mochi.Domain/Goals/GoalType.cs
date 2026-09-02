namespace Mochi.Domain.Goals;

/// <summary>What a goal matches against.</summary>
public enum GoalType
{
    /// <summary>Visits to a specific path.</summary>
    Page,

    /// <summary>Occurrences of a named custom event.</summary>
    Event,

    /// <summary>Clicks on outbound links.</summary>
    Outbound,

    /// <summary>File downloads.</summary>
    Download,
}
