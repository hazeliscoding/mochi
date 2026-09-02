namespace Mochi.Domain.Sites;

/// <summary>
/// How long a site keeps its daily aggregates. Raw events always expire after
/// seven days regardless of this setting (ADR 0003).
/// </summary>
public enum RetentionPolicy
{
    /// <summary>Keep aggregates for 30 days.</summary>
    Days30,

    /// <summary>Keep aggregates for 90 days.</summary>
    Days90,

    /// <summary>Keep aggregates for one year.</summary>
    OneYear,

    /// <summary>Keep aggregates forever. Safe because they contain counts, never per-visit rows.</summary>
    Unlimited,
}
