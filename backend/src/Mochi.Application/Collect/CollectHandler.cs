using Mochi.Application.Abstractions;
using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Application.Collect;

/// <summary>
/// Ingests one beacon: validate the site, scrub the payload, hash the visitor,
/// store the event. Everything personal (IP, raw user agent, query strings,
/// full referrer) is reduced or dropped here and never reaches storage.
/// </summary>
public sealed class CollectHandler(
    ISiteRepository sites,
    IAnalyticsEventStore events,
    IDailySaltProvider salt,
    IUserAgentParser uaParser,
    IGeoLocator geo,
    IClock clock)
{
    /// <summary>Handles one beacon. Never throws on bad input; returns a drop reason instead.</summary>
    public async Task<CollectResult> HandleAsync(CollectCommand cmd, CancellationToken ct = default)
    {
        if (!SiteId.TryParse(cmd.SiteId, out var siteId))
            return CollectResult.Dropped("malformed site id");

        var site = await sites.GetAsync(siteId, ct);
        if (site is null)
            return CollectResult.Dropped("unknown site id");

        var isPageview = cmd.Type == "pageview";
        if (!isPageview && cmd.Type != "event")
            return CollectResult.Dropped("unknown event type");
        if (!isPageview && string.IsNullOrWhiteSpace(cmd.EventName))
            return CollectResult.Dropped("custom event without a name");

        var (path, campaign) = SplitPath(cmd.Path);
        var (channel, referrerDomain) = ReferrerClassifier.Classify(cmd.Referrer);
        var (device, browser, os) = uaParser.Parse(cmd.UserAgent);
        var visitor = VisitorHash.Compute(salt.CurrentSalt, siteId, cmd.IpAddress, cmd.UserAgent);
        var country = geo.CountryCode(cmd.IpAddress);
        var now = clock.UtcNow;

        var evt = isPageview
            ? AnalyticsEvent.Pageview(siteId, visitor, path, referrerDomain, channel, campaign, country, device, browser, os, now)
            : AnalyticsEvent.Custom(siteId, visitor, cmd.EventName!, path, referrerDomain, channel, campaign, country, device, browser, os, now);

        await events.AppendAsync(evt, ct);
        return CollectResult.Accepted;
    }

    /// <summary>
    /// Strips the query string from the path, keeping only the utm_campaign
    /// value. Query strings can carry personal data and are never stored.
    /// </summary>
    private static (string Path, string? Campaign) SplitPath(string? rawPath)
    {
        var raw = string.IsNullOrWhiteSpace(rawPath) ? "/" : rawPath;
        var queryStart = raw.IndexOf('?');
        if (queryStart < 0) return (raw, null);

        string? campaign = null;
        foreach (var pair in raw[(queryStart + 1)..].Split('&'))
        {
            if (pair.StartsWith("utm_campaign=", StringComparison.OrdinalIgnoreCase))
            {
                campaign = Uri.UnescapeDataString(pair["utm_campaign=".Length..]);
                break;
            }
        }

        return (raw[..queryStart], campaign);
    }
}
