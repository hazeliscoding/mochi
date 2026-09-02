namespace Mochi.Application.Abstractions;

/// <summary>
/// Provides the daily hashing salt (ADR 0001). Implementations must rotate at
/// the UTC day boundary and must not persist or expose previous salts.
/// </summary>
public interface IDailySaltProvider
{
    /// <summary>The salt for the current UTC day.</summary>
    ReadOnlySpan<byte> CurrentSalt { get; }
}
