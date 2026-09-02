namespace Mochi.Application.Abstractions;

/// <summary>Resolves an IP address to a country. The IP is dropped after this call.</summary>
public interface IGeoLocator
{
    /// <summary>ISO 3166-1 alpha-2 country code, or null if unresolved.</summary>
    string? CountryCode(string ipAddress);
}
