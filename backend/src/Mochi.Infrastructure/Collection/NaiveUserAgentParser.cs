using Mochi.Application.Abstractions;
using Mochi.Domain.Collection;

namespace Mochi.Infrastructure.Collection;

/// <summary>
/// Substring-based user agent parser. Placeholder until a real parser library
/// is chosen; good enough for family-level stats in development.
/// </summary>
public sealed class NaiveUserAgentParser : IUserAgentParser
{
    /// <inheritdoc />
    public (DeviceClass Device, string Browser, string Os) Parse(string userAgent)
    {
        var ua = userAgent ?? string.Empty;

        var device = DeviceClass.Desktop;
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) || ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            device = DeviceClass.Tablet;
        else if (ua.Contains("Mobi", StringComparison.OrdinalIgnoreCase) || ua.Contains("Android", StringComparison.OrdinalIgnoreCase))
            device = DeviceClass.Mobile;

        var browser =
            ua.Contains("Firefox", StringComparison.OrdinalIgnoreCase) ? "Firefox" :
            ua.Contains("Edg/", StringComparison.Ordinal) ? "Edge" :
            ua.Contains("OPR/", StringComparison.Ordinal) ? "Opera" :
            ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ? "Chrome" :
            ua.Contains("Safari", StringComparison.OrdinalIgnoreCase) ? "Safari" :
            "Other";

        var os =
            ua.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows" :
            ua.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android" :
            ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iOS" :
            ua.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) ? "macOS" :
            ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux" :
            "Other";

        return (device, browser, os);
    }
}
