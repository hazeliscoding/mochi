using System.Net;
using MaxMind.GeoIP2;
using Mochi.Application.Abstractions;

namespace Mochi.Infrastructure.Collection;

/// <summary>
/// Country lookup against a local GeoLite2-Country.mmdb file. The database
/// path comes from configuration (Mochi:GeoIpDatabase); when unset, DI falls
/// back to <see cref="NullGeoLocator"/>. Lookups are in-process, no network.
/// </summary>
public sealed class MaxMindGeoLocator(string databasePath) : IGeoLocator, IDisposable
{
    private readonly DatabaseReader _reader = new(databasePath);

    /// <inheritdoc />
    public string? CountryCode(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip)) return null;
        return _reader.TryCountry(ip, out var response) ? response?.Country.IsoCode : null;
    }

    /// <inheritdoc />
    public void Dispose() => _reader.Dispose();
}
