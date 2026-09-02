namespace Mochi.Application.Collect;

/// <summary>
/// Outcome of a collect call. The HTTP response is 202 either way; drops are
/// only visible in server logs (ADR 0002).
/// </summary>
/// <param name="Stored">True if an event was written.</param>
/// <param name="DropReason">Why the beacon was dropped. Null when stored.</param>
public sealed record CollectResult(bool Stored, string? DropReason)
{
    /// <summary>The beacon was stored.</summary>
    public static readonly CollectResult Accepted = new(true, null);

    /// <summary>The beacon was dropped. The reason goes to logs, never to the client.</summary>
    public static CollectResult Dropped(string reason) => new(false, reason);
}
