namespace Mochi.Application.Abstractions;

/// <summary>Clock port so handlers can be tested with a fixed time.</summary>
public interface IClock
{
    /// <summary>Current instant.</summary>
    DateTimeOffset UtcNow { get; }
}
