namespace Mochi.Domain.Collection;

/// <summary>Kind of tracked event.</summary>
public enum EventType
{
    /// <summary>A page was viewed.</summary>
    Pageview,

    /// <summary>A named custom event, sent via mochi('event', name).</summary>
    Custom,
}
