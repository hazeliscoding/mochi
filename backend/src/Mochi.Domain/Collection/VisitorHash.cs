using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Mochi.Domain.Sites;

namespace Mochi.Domain.Collection;

/// <summary>
/// Day-scoped visitor identifier per ADR 0001. The first 8 bytes of
/// SHA-256(salt || site id || ip || user agent). The salt rotates daily and is
/// destroyed, so hashes cannot be linked across days and cannot be reversed
/// to an IP. The IP and raw user agent are never persisted.
/// </summary>
public readonly record struct VisitorHash
{
    private VisitorHash(ulong value) => Value = value;

    /// <summary>The 8-byte hash as an unsigned integer.</summary>
    public ulong Value { get; }

    /// <summary>Computes the hash for one visitor on one site under the current daily salt.</summary>
    public static VisitorHash Compute(ReadOnlySpan<byte> dailySalt, SiteId site, string ipAddress, string userAgent)
    {
        var input = new byte[dailySalt.Length + Encoding.UTF8.GetByteCount(site.Value + ipAddress + userAgent)];
        dailySalt.CopyTo(input);
        Encoding.UTF8.GetBytes(site.Value + ipAddress + userAgent, input.AsSpan(dailySalt.Length));

        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(input, digest);
        return new VisitorHash(BinaryPrimitives.ReadUInt64BigEndian(digest));
    }
}
