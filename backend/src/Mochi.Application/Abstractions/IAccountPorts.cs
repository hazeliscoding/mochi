using Mochi.Domain.Accounts;
using Mochi.Domain.Sites;

namespace Mochi.Application.Abstractions;

/// <summary>Persistence port for accounts.</summary>
public interface IUserRepository
{
    /// <summary>Returns the user or null. Email is matched lowercase.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns the user or null.</summary>
    Task<User?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>Total number of accounts. Zero means setup mode (ADR 0004).</summary>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Adds an account.</summary>
    Task AddAsync(User user, CancellationToken ct = default);
}

/// <summary>Persistence port for sessions.</summary>
public interface ISessionStore
{
    /// <summary>Adds a session.</summary>
    Task AddAsync(Session session, CancellationToken ct = default);

    /// <summary>Returns the session with this token hash, or null.</summary>
    Task<Session?> GetByTokenHashAsync(byte[] tokenHash, CancellationToken ct = default);

    /// <summary>Persists a slid expiry window.</summary>
    Task UpdateAsync(Session session, CancellationToken ct = default);

    /// <summary>Deletes one session. Logout.</summary>
    Task RemoveAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Deletes every session of a user. Log out everywhere.</summary>
    Task RemoveAllForUserAsync(string userId, CancellationToken ct = default);
}

/// <summary>Persistence port for site memberships.</summary>
public interface IMembershipRepository
{
    /// <summary>True when the user holds any role on the site.</summary>
    Task<bool> IsMemberAsync(string userId, SiteId siteId, CancellationToken ct = default);

    /// <summary>Site ids the user is a member of.</summary>
    Task<IReadOnlyList<SiteId>> ListSiteIdsAsync(string userId, CancellationToken ct = default);

    /// <summary>Adds a membership.</summary>
    Task AddAsync(SiteMembership membership, CancellationToken ct = default);

    /// <summary>
    /// Makes the user owner of every site that has no members. Used once by
    /// first-run setup to claim sites registered before auth existed.
    /// </summary>
    Task ClaimOrphanedSitesAsync(string userId, CancellationToken ct = default);
}

/// <summary>Password hashing port. Implemented with PasswordHasher V3 (ADR 0004).</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a password.</summary>
    string Hash(string password);

    /// <summary>Verifies a password against a stored hash.</summary>
    bool Verify(string hash, string password);
}

/// <summary>
/// The one-time first-run setup code (ADR 0004). Taken from MOCHI_SETUP_CODE
/// or generated at startup and printed to stdout.
/// </summary>
public interface ISetupCodeProvider
{
    /// <summary>The code required by POST /api/auth/setup.</summary>
    string Code { get; }
}
