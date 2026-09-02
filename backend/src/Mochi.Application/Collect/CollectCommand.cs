namespace Mochi.Application.Collect;

/// <summary>
/// One incoming beacon from mochi.js plus the server-derived request context.
/// The client never sends the IP or user agent; the API layer fills them in
/// from the connection and both are dropped after hashing (ADR 0002).
/// </summary>
/// <param name="SiteId">Public site id from the payload.</param>
/// <param name="Type">"pageview" or "event".</param>
/// <param name="Path">Page path, may still carry a query string.</param>
/// <param name="EventName">Custom event name. Null for pageviews.</param>
/// <param name="Referrer">Full referrer URL as sent by the browser. Reduced before storage.</param>
/// <param name="IpAddress">Caller IP, server-derived. Never persisted.</param>
/// <param name="UserAgent">Raw user agent, server-derived. Never persisted.</param>
public sealed record CollectCommand(
    string SiteId,
    string Type,
    string? Path,
    string? EventName,
    string? Referrer,
    string IpAddress,
    string UserAgent);
