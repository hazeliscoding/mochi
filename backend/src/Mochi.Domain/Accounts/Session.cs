namespace Mochi.Domain.Accounts;

/// <summary>
/// A dashboard session (ADR 0004). The cookie carries an opaque random token;
/// only its SHA-256 hash is stored, so a database leak reveals no usable
/// session credentials. Sliding 14-day expiry under a 30-day absolute cap.
/// </summary>
public sealed class Session
{
    /// <summary>Idle lifetime. Each authenticated request slides the expiry.</summary>
    public static readonly TimeSpan SlidingLifetime = TimeSpan.FromDays(14);

    /// <summary>Hard cap regardless of activity.</summary>
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(30);

    private Session(string id, string userId, byte[] tokenHash, DateTimeOffset createdAt, DateTimeOffset lastSeenAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        LastSeenAt = lastSeenAt;
    }

    /// <summary>Opaque session id, prefixed "s_".</summary>
    public string Id { get; }

    /// <summary>The account this session belongs to.</summary>
    public string UserId { get; }

    /// <summary>SHA-256 of the cookie token. The token itself is never stored.</summary>
    public byte[] TokenHash { get; }

    /// <summary>When the session was created. Anchors the absolute cap.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Last authenticated request. Anchors the sliding expiry.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Creates a session for a freshly issued token.</summary>
    public static Session Create(string userId, byte[] tokenHash, DateTimeOffset now)
        => new($"s_{Guid.NewGuid():N}", userId, tokenHash, now, now);

    /// <summary>True when both the sliding and absolute windows are still open.</summary>
    public bool IsAlive(DateTimeOffset now)
        => now < LastSeenAt + SlidingLifetime && now < CreatedAt + AbsoluteLifetime;

    /// <summary>Slides the expiry window.</summary>
    public void Touch(DateTimeOffset now) => LastSeenAt = now;
}
