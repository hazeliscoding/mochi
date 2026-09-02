using System.Security.Cryptography;
using System.Text;
using Mochi.Application.Abstractions;
using Mochi.Domain.Accounts;

namespace Mochi.Application.Auth;

/// <summary>Outcome of setup or login. Token is set only on success.</summary>
/// <param name="Token">Raw session token for the cookie. Null on failure.</param>
/// <param name="Error">Human-readable failure reason. Null on success.</param>
public sealed record AuthResult(string? Token, string? Error)
{
    /// <summary>Successful outcome carrying the session token.</summary>
    public static AuthResult Ok(string token) => new(token, null);

    /// <summary>Failed outcome. The reason is safe to return to the client.</summary>
    public static AuthResult Fail(string error) => new(null, error);
}

/// <summary>Setup, login, logout and session validation (ADR 0004).</summary>
public sealed class AuthService(
    IUserRepository users,
    ISessionStore sessions,
    IMembershipRepository memberships,
    IPasswordHasher hasher,
    ISetupCodeProvider setupCode,
    IClock clock)
{
    private const int MinPasswordLength = 10;

    /// <summary>
    /// Hash compared for unknown emails so login duration does not reveal
    /// whether an account exists. Cached process-wide because hashing is
    /// deliberately slow and the service is scoped.
    /// </summary>
    private static string? _dummyHash;

    private string DummyHash => _dummyHash ??= hasher.Hash(Guid.NewGuid().ToString("N"));

    /// <summary>True while no account exists, which is when /api/auth/setup is open.</summary>
    public async Task<bool> NeedsSetupAsync(CancellationToken ct = default)
        => await users.CountAsync(ct) == 0;

    /// <summary>
    /// Creates the first admin account. Requires the setup code, only works
    /// while zero accounts exist, and claims any sites registered before auth.
    /// </summary>
    public async Task<AuthResult> SetupAsync(string? code, string? email, string? password, CancellationToken ct = default)
    {
        if (!FixedEquals(code ?? string.Empty, setupCode.Code)) return AuthResult.Fail("invalid setup code");
        if (await users.CountAsync(ct) > 0) return AuthResult.Fail("setup is already complete");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return AuthResult.Fail("a valid email is required");
        if (password is null || password.Length < MinPasswordLength) return AuthResult.Fail($"password must be at least {MinPasswordLength} characters");

        var user = User.Create(email, hasher.Hash(password), isAdmin: true, clock.UtcNow);
        await users.AddAsync(user, ct);
        await memberships.ClaimOrphanedSitesAsync(user.Id, ct);
        return AuthResult.Ok(await IssueSessionAsync(user.Id, ct));
    }

    /// <summary>Verifies credentials. The failure message never distinguishes unknown email from wrong password.</summary>
    public async Task<AuthResult> LoginAsync(string? email, string? password, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync((email ?? string.Empty).Trim().ToLowerInvariant(), ct);
        var valid = user is null
            ? hasher.Verify(DummyHash, password ?? string.Empty)
            : hasher.Verify(user.PasswordHash, password ?? string.Empty);
        if (user is null || !valid) return AuthResult.Fail("invalid email or password");

        return AuthResult.Ok(await IssueSessionAsync(user.Id, ct));
    }

    /// <summary>Resolves a cookie token to its user, sliding the session. Null when invalid or expired.</summary>
    public async Task<User?> AuthenticateAsync(string rawToken, CancellationToken ct = default)
    {
        var session = await sessions.GetByTokenHashAsync(SessionToken.Hash(rawToken), ct);
        if (session is null) return null;

        var now = clock.UtcNow;
        if (!session.IsAlive(now))
        {
            await sessions.RemoveAsync(session.Id, ct);
            return null;
        }

        session.Touch(now);
        await sessions.UpdateAsync(session, ct);
        return await users.GetAsync(session.UserId, ct);
    }

    /// <summary>Deletes the session behind a cookie token. Safe to call with an invalid token.</summary>
    public async Task LogoutAsync(string rawToken, CancellationToken ct = default)
    {
        var session = await sessions.GetByTokenHashAsync(SessionToken.Hash(rawToken), ct);
        if (session is not null) await sessions.RemoveAsync(session.Id, ct);
    }

    private async Task<string> IssueSessionAsync(string userId, CancellationToken ct)
    {
        var (raw, hash) = SessionToken.Create();
        await sessions.AddAsync(Session.Create(userId, hash, clock.UtcNow), ct);
        return raw;
    }

    private static bool FixedEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
