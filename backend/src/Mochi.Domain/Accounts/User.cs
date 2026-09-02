namespace Mochi.Domain.Accounts;

/// <summary>
/// A dashboard account (ADR 0004). Deliberately minimal PII: an email and a
/// password hash, nothing more.
/// </summary>
public sealed class User
{
    private User(string id, string email, string passwordHash, bool isAdmin, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        IsAdmin = isAdmin;
        CreatedAt = createdAt;
    }

    /// <summary>Opaque user id, prefixed "u_".</summary>
    public string Id { get; }

    /// <summary>Login email, stored lowercase.</summary>
    public string Email { get; }

    /// <summary>Password hash in the hasher's own versioned format.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>True for the setup account. Gates the admin endpoints.</summary>
    public bool IsAdmin { get; }

    /// <summary>When the account was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Creates an account. The caller hashes the password first.</summary>
    /// <exception cref="ArgumentException">Email or hash is empty.</exception>
    public static User Create(string email, string passwordHash, bool isAdmin, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return new User($"u_{Guid.NewGuid():N}", email.Trim().ToLowerInvariant(), passwordHash, isAdmin, now);
    }
}
