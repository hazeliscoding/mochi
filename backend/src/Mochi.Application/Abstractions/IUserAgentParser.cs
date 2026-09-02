using Mochi.Domain.Collection;

namespace Mochi.Application.Abstractions;

/// <summary>Reduces a raw user agent to the coarse families that get stored.</summary>
public interface IUserAgentParser
{
    /// <summary>Parses <paramref name="userAgent"/>. Unknown agents map to Desktop / "Other" / "Other".</summary>
    (DeviceClass Device, string Browser, string Os) Parse(string userAgent);
}
