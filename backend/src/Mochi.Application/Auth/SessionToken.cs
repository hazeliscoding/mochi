using System.Security.Cryptography;

namespace Mochi.Application.Auth;

/// <summary>
/// The opaque session credential. The raw value goes into the cookie; only
/// the SHA-256 hash is stored (ADR 0004).
/// </summary>
public static class SessionToken
{
    /// <summary>Creates a fresh 256-bit token.</summary>
    public static (string Raw, byte[] Hash) Create()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (raw, Hash(raw));
    }

    /// <summary>Hashes a raw token for storage or lookup.</summary>
    public static byte[] Hash(string raw) => SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
}
