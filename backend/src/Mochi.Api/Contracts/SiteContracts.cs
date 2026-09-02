using Mochi.Domain.Sites;

namespace Mochi.Api.Contracts;

/// <summary>Body for POST /api/sites and PUT /api/sites/{id}.</summary>
/// <param name="Name">Display name.</param>
/// <param name="Domain">Site domain, scheme optional.</param>
/// <param name="Timezone">IANA timezone id.</param>
/// <param name="Retention">"30d", "90d", "1y" or "unlimited". Null keeps the current value.</param>
public sealed record SiteRequest(string? Name, string? Domain, string? Timezone, string? Retention);

/// <summary>One entry of GET /api/sites, with live numbers for the Websites page.</summary>
/// <param name="Site">The site itself.</param>
/// <param name="ViewsLast30d">Pageviews in the last 30 days including today.</param>
/// <param name="ActiveNow">Distinct visitors in the last 5 minutes.</param>
/// <param name="Status">"active" once traffic has been seen in the period, else "waiting".</param>
public sealed record SiteListItem(SiteResponse Site, long ViewsLast30d, int ActiveNow, string Status);

/// <summary>Site as returned by the sites endpoints.</summary>
/// <param name="Id">Public site id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Domain">Registered domain.</param>
/// <param name="Timezone">IANA timezone id.</param>
/// <param name="Retention">Retention setting in wire form.</param>
/// <param name="Snippet">Copy-paste tracking snippet for this site.</param>
public sealed record SiteResponse(string Id, string Name, string Domain, string Timezone, string Retention, string Snippet)
{
    /// <summary>Maps the aggregate to the wire shape.</summary>
    public static SiteResponse From(Site site, string snippetBaseUrl) => new(
        site.Id.Value,
        site.Name,
        site.Domain,
        site.Timezone,
        ToWire(site.Retention),
        $"<script defer src=\"{snippetBaseUrl}/script.js\" data-site=\"{site.Id.Value}\"></script>");

    /// <summary>Parses the wire retention value. Returns null for unknown input.</summary>
    public static RetentionPolicy? ParseRetention(string? wire) => wire switch
    {
        "30d" => RetentionPolicy.Days30,
        "90d" => RetentionPolicy.Days90,
        "1y" => RetentionPolicy.OneYear,
        "unlimited" => RetentionPolicy.Unlimited,
        _ => null,
    };

    private static string ToWire(RetentionPolicy retention) => retention switch
    {
        RetentionPolicy.Days30 => "30d",
        RetentionPolicy.Days90 => "90d",
        RetentionPolicy.OneYear => "1y",
        _ => "unlimited",
    };
}
