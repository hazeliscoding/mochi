using Mochi.Application.Abstractions;
using Mochi.Domain.Collection;
using UAParser;

namespace Mochi.Infrastructure.Collection;

/// <summary>
/// User agent parser backed by the uap-core regex database (UAParser package).
/// Family names only; versions are parsed but never stored. Device class still
/// uses substring heuristics because uap device families do not map cleanly to
/// desktop/mobile/tablet.
/// </summary>
public sealed class UaParserUserAgentParser : IUserAgentParser
{
    private readonly Parser _parser = Parser.GetDefault();

    /// <inheritdoc />
    public (DeviceClass Device, string Browser, string Os) Parse(string userAgent)
    {
        var ua = userAgent ?? string.Empty;
        var info = _parser.Parse(ua);

        var device = DeviceClass.Desktop;
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) || ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            device = DeviceClass.Tablet;
        else if (ua.Contains("Mobi", StringComparison.OrdinalIgnoreCase) || info.OS.Family is "Android" or "iOS")
            device = DeviceClass.Mobile;

        var browser = info.UA.Family is "Other" or "" ? "Other" : info.UA.Family;
        var os = info.OS.Family is "Other" or "" ? "Other" : NormalizeOs(info.OS.Family);
        return (device, browser, os);
    }

    private static string NormalizeOs(string family) => family switch
    {
        "Mac OS X" => "macOS",
        var f when f.StartsWith("Windows", StringComparison.Ordinal) => "Windows",
        var f when f.Contains("Linux", StringComparison.OrdinalIgnoreCase) || f is "Ubuntu" or "Fedora" or "Debian" or "Arch Linux" => "Linux",
        _ => family,
    };
}
