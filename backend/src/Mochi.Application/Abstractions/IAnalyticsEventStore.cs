using Mochi.Domain.Collection;

namespace Mochi.Application.Abstractions;

/// <summary>Append-only store for raw scrubbed events.</summary>
public interface IAnalyticsEventStore
{
    /// <summary>Appends one event.</summary>
    Task AppendAsync(AnalyticsEvent evt, CancellationToken ct = default);
}
