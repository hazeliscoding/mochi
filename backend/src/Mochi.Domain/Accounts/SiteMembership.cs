using Mochi.Domain.Sites;

namespace Mochi.Domain.Accounts;

/// <summary>Role a user holds on a site. Editor and Viewer arrive with teams.</summary>
public enum SiteRole
{
    /// <summary>Full control of the site and its data.</summary>
    Owner,
}

/// <summary>
/// Membership of a user on a site (ADR 0004). Sites are only ever loaded
/// through this join; there is no other path from a user to a site.
/// </summary>
/// <param name="UserId">The member.</param>
/// <param name="SiteId">The site.</param>
/// <param name="Role">The member's role.</param>
public sealed record SiteMembership(string UserId, SiteId SiteId, SiteRole Role);
