using System.Security.Cryptography;

namespace Mochi.Domain.Sites;

/// <summary>
/// Public site identifier in the form "MC-" plus five base32 characters.
/// Appears in customer page source, so it is an identifier, not a secret.
/// </summary>
public readonly record struct SiteId
{
    /// <summary>Crockford base32 alphabet. Excludes I, L, O and U to avoid misreading.</summary>
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private const string Prefix = "MC-";
    private const int RandomLength = 5;

    private SiteId(string value) => Value = value;

    /// <summary>The full identifier string, for example "MC-7F3K2".</summary>
    public string Value { get; }

    /// <summary>Creates a new random identifier.</summary>
    public static SiteId New()
    {
        var chars = RandomNumberGenerator.GetItems<char>(Alphabet, RandomLength);
        return new SiteId(Prefix + new string(chars));
    }

    /// <summary>Parses <paramref name="value"/>. Returns false if the format is invalid.</summary>
    public static bool TryParse(string? value, out SiteId id)
    {
        id = default;
        if (value is null || value.Length != Prefix.Length + RandomLength) return false;
        if (!value.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        for (var i = Prefix.Length; i < value.Length; i++)
        {
            if (!Alphabet.Contains(value[i])) return false;
        }

        id = new SiteId(value);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
