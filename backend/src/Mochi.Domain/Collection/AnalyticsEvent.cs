using Mochi.Domain.Sites;

namespace Mochi.Domain.Collection;

/// <summary>
/// A single scrubbed event as stored in the raw events table. Contains no IP,
/// no raw user agent, no full referrer URL and no query strings (ADR 0003).
/// Raw events expire after seven days; rollups carry the history.
/// </summary>
public sealed class AnalyticsEvent
{
    private AnalyticsEvent(
        SiteId siteId,
        VisitorHash visitor,
        EventType type,
        string path,
        string? eventName,
        string? referrerDomain,
        Channel channel,
        string? campaign,
        string? countryCode,
        DeviceClass deviceClass,
        string browser,
        string os,
        DateTimeOffset occurredAt)
    {
        SiteId = siteId;
        Visitor = visitor;
        Type = type;
        Path = path;
        EventName = eventName;
        ReferrerDomain = referrerDomain;
        Channel = channel;
        Campaign = campaign;
        CountryCode = countryCode;
        DeviceClass = deviceClass;
        Browser = browser;
        Os = os;
        OccurredAt = occurredAt;
    }

    /// <summary>Site the event belongs to.</summary>
    public SiteId SiteId { get; }

    /// <summary>Day-scoped visitor hash. Meaningless across day boundaries.</summary>
    public VisitorHash Visitor { get; }

    /// <summary>Pageview or custom event.</summary>
    public EventType Type { get; }

    /// <summary>Path without query string.</summary>
    public string Path { get; }

    /// <summary>Custom event name. Null for pageviews.</summary>
    public string? EventName { get; }

    /// <summary>Referrer domain only. Null for direct traffic.</summary>
    public string? ReferrerDomain { get; }

    /// <summary>Channel classified from the referrer.</summary>
    public Channel Channel { get; }

    /// <summary>Campaign from UTM parameters, if any.</summary>
    public string? Campaign { get; }

    /// <summary>ISO 3166-1 alpha-2 country code from GeoIP. Null when unresolved.</summary>
    public string? CountryCode { get; }

    /// <summary>Coarse device class.</summary>
    public DeviceClass DeviceClass { get; }

    /// <summary>Browser family only, for example "Firefox".</summary>
    public string Browser { get; }

    /// <summary>Operating system family only, for example "Windows".</summary>
    public string Os { get; }

    /// <summary>Server-side receive time.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Creates a pageview event.</summary>
    public static AnalyticsEvent Pageview(
        SiteId siteId,
        VisitorHash visitor,
        string path,
        string? referrerDomain,
        Channel channel,
        string? campaign,
        string? countryCode,
        DeviceClass deviceClass,
        string browser,
        string os,
        DateTimeOffset occurredAt)
        => new(siteId, visitor, EventType.Pageview, path, null, referrerDomain, channel, campaign, countryCode, deviceClass, browser, os, occurredAt);

    /// <summary>Creates a custom event.</summary>
    /// <exception cref="ArgumentException">Event name is empty.</exception>
    public static AnalyticsEvent Custom(
        SiteId siteId,
        VisitorHash visitor,
        string eventName,
        string path,
        string? referrerDomain,
        Channel channel,
        string? campaign,
        string? countryCode,
        DeviceClass deviceClass,
        string browser,
        string os,
        DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        return new(siteId, visitor, EventType.Custom, path, eventName.Trim(), referrerDomain, channel, campaign, countryCode, deviceClass, browser, os, occurredAt);
    }
}
