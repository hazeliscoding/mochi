using Mochi.Application.Abstractions;
using Mochi.Domain.Sites;

namespace Mochi.Application.Sites;

/// <summary>Registers a new site and returns the aggregate for the API to shape.</summary>
public sealed class RegisterSiteHandler(ISiteRepository sites, IClock clock)
{
    /// <summary>Creates and persists the site.</summary>
    /// <exception cref="ArgumentException">Name, domain or timezone is empty.</exception>
    public async Task<Site> HandleAsync(string name, string domain, string timezone, CancellationToken ct = default)
    {
        var site = Site.Register(name, domain, timezone, clock.UtcNow);
        await sites.AddAsync(site, ct);
        return site;
    }
}
