using Mochi.Domain.Collection;

namespace Mochi.Application.Collect;

/// <summary>
/// Reduces a full referrer URL to a domain and a channel. Full referrer URLs
/// are never stored (ADR 0002). Lists are seed data, not exhaustive.
/// </summary>
public static class ReferrerClassifier
{
    private static readonly string[] SearchDomains =
    [
        "google.", "bing.com", "duckduckgo.com", "search.brave.com", "ecosia.org", "yandex.", "baidu.com",
    ];

    private static readonly string[] SocialDomains =
    [
        "facebook.com", "instagram.com", "twitter.com", "x.com", "t.co", "linkedin.com",
        "reddit.com", "news.ycombinator.com", "mastodon.", "bsky.app", "youtube.com", "tiktok.com",
    ];

    /// <summary>Classifies <paramref name="referrer"/>. Null, empty or unparseable means direct.</summary>
    public static (Channel Channel, string? Domain) Classify(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer)) return (Channel.Direct, null);
        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri)) return (Channel.Direct, null);

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];

        if (SearchDomains.Any(host.Contains)) return (Channel.Search, host);
        if (SocialDomains.Any(host.Contains)) return (Channel.Social, host);
        return (Channel.Referral, host);
    }
}
