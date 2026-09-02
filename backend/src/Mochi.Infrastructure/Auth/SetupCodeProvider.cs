using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Mochi.Application.Abstractions;

namespace Mochi.Infrastructure.Auth;

/// <summary>
/// First-run setup code (ADR 0004). MOCHI_SETUP_CODE from the environment
/// wins; otherwise a random code is generated once per process. Program.cs
/// prints it at startup while setup is still open.
/// </summary>
public sealed class SetupCodeProvider(IConfiguration config) : ISetupCodeProvider
{
    private readonly Lazy<string> _code = new(() =>
        config["MOCHI_SETUP_CODE"] is { Length: > 0 } configured
            ? configured
            : Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8)));

    /// <inheritdoc />
    public string Code => _code.Value;
}
