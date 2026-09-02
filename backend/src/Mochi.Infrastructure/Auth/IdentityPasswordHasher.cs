using Microsoft.AspNetCore.Identity;
using Mochi.Application.Abstractions;

namespace Mochi.Infrastructure.Auth;

/// <summary>
/// ASP.NET Core PasswordHasher V3: PBKDF2-HMAC-SHA512 with a versioned,
/// self-describing hash format, so parameters can be raised later without a
/// migration (ADR 0004).
/// </summary>
public sealed class IdentityPasswordHasher : Application.Abstractions.IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object Dummy = new();

    /// <inheritdoc />
    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    /// <inheritdoc />
    public bool Verify(string hash, string password)
        => _hasher.VerifyHashedPassword(Dummy, hash, password) is not PasswordVerificationResult.Failed;
}
