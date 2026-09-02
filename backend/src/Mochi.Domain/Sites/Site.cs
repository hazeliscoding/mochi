namespace Mochi.Domain.Sites;

/// <summary>
/// A website registered for analytics. Aggregate root for site settings.
/// </summary>
public sealed class Site
{
    private Site(SiteId id, string name, string domain, string timezone, RetentionPolicy retention, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Domain = domain;
        Timezone = timezone;
        Retention = retention;
        CreatedAt = createdAt;
    }

    /// <summary>Public identifier used by the tracking snippet.</summary>
    public SiteId Id { get; }

    /// <summary>Display name shown in the dashboard.</summary>
    public string Name { get; private set; }

    /// <summary>Registered domain, without scheme, for example "hazeliscoding.com".</summary>
    public string Domain { get; private set; }

    /// <summary>IANA timezone id used to bucket dashboard dates, for example "Europe/Berlin".</summary>
    public string Timezone { get; private set; }

    /// <summary>Aggregate retention setting. See <see cref="RetentionPolicy"/>.</summary>
    public RetentionPolicy Retention { get; private set; }

    /// <summary>When the site was registered.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Registers a new site with a fresh id and unlimited retention.</summary>
    /// <exception cref="ArgumentException">Name, domain or timezone is empty.</exception>
    public static Site Register(string name, string domain, string timezone, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone);
        return new Site(SiteId.New(), name.Trim(), NormalizeDomain(domain), timezone, RetentionPolicy.Unlimited, now);
    }

    /// <summary>Updates the settings a site owner can change.</summary>
    /// <exception cref="ArgumentException">Name or timezone is empty.</exception>
    public void UpdateSettings(string name, string timezone, RetentionPolicy retention)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone);
        Name = name.Trim();
        Timezone = timezone;
        Retention = retention;
    }

    private static string NormalizeDomain(string domain)
    {
        var d = domain.Trim().ToLowerInvariant();
        if (d.StartsWith("http://", StringComparison.Ordinal)) d = d[7..];
        if (d.StartsWith("https://", StringComparison.Ordinal)) d = d[8..];
        return d.TrimEnd('/');
    }
}
