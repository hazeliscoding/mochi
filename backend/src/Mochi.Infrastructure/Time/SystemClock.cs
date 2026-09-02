using Mochi.Application.Abstractions;

namespace Mochi.Infrastructure.Time;

/// <summary>System clock.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
