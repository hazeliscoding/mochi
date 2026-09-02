namespace Mochi.Domain.Collection;

/// <summary>Traffic channel derived from the referrer at ingest.</summary>
public enum Channel
{
    /// <summary>No referrer.</summary>
    Direct,

    /// <summary>Referrer is a known search engine.</summary>
    Search,

    /// <summary>Referrer is any other site.</summary>
    Referral,

    /// <summary>Referrer is a known social network.</summary>
    Social,
}
