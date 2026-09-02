namespace Mochi.Api.Contracts;

/// <summary>
/// Beacon body sent by mochi.js as text/plain JSON. Field names match the
/// wire format in ADR 0002.
/// </summary>
/// <param name="Site">Public site id.</param>
/// <param name="Type">"pageview" or "event".</param>
/// <param name="Path">Page path.</param>
/// <param name="Name">Custom event name. Null for pageviews.</param>
/// <param name="Referrer">Full referrer URL. Reduced server-side, never stored.</param>
public sealed record CollectPayload(string? Site, string? Type, string? Path, string? Name, string? Referrer);
