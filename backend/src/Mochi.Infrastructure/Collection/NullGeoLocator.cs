using Mochi.Application.Abstractions;

namespace Mochi.Infrastructure.Collection;

/// <summary>Placeholder geo lookup until a GeoIP database is wired in. Always returns null.</summary>
public sealed class NullGeoLocator : IGeoLocator
{
    /// <inheritdoc />
    public string? CountryCode(string ipAddress) => null;
}
