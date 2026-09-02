using System.Security.Cryptography;
using Mochi.Application.Abstractions;

namespace Mochi.Infrastructure.Collection;

/// <summary>
/// In-memory daily salt (ADR 0001). A new 32-byte salt is generated on first
/// use each UTC day; the previous one is overwritten and never persisted.
/// A restart also discards the salt, which only splits that day's visitors,
/// an accepted inaccuracy.
/// </summary>
public sealed class RotatingDailySaltProvider(IClock clock) : IDailySaltProvider
{
    private readonly Lock _lock = new();
    private byte[] _salt = [];
    private DateOnly _saltDay = DateOnly.MinValue;

    /// <inheritdoc />
    public ReadOnlySpan<byte> CurrentSalt
    {
        get
        {
            lock (_lock)
            {
                var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
                if (today != _saltDay)
                {
                    _salt = RandomNumberGenerator.GetBytes(32);
                    _saltDay = today;
                }

                return _salt;
            }
        }
    }
}
