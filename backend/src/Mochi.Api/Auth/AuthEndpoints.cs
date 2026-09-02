using Mochi.Application.Auth;

namespace Mochi.Api.Auth;

/// <summary>Body for POST /api/auth/setup.</summary>
/// <param name="Code">The one-time setup code from the server log.</param>
/// <param name="Email">Admin email.</param>
/// <param name="Password">Admin password.</param>
public sealed record SetupRequest(string? Code, string? Email, string? Password);

/// <summary>Body for POST /api/auth/login.</summary>
/// <param name="Email">Account email.</param>
/// <param name="Password">Account password.</param>
public sealed record LoginRequest(string? Email, string? Password);

/// <summary>Response of GET /api/auth/status.</summary>
/// <param name="NeedsSetup">True while no account exists.</param>
/// <param name="Authenticated">True when the request carries a live session.</param>
/// <param name="Email">The session's account email. Null when anonymous.</param>
/// <param name="IsAdmin">True for the setup account. Null when anonymous.</param>
public sealed record AuthStatusResponse(bool NeedsSetup, bool Authenticated, string? Email, bool? IsAdmin);

/// <summary>The /api/auth endpoints (ADR 0004).</summary>
public static class AuthEndpoints
{
    /// <summary>Maps status, setup, login and logout.</summary>
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapGet("/status", async (HttpContext ctx, AuthService svc, CancellationToken ct) =>
        {
            var user = await SessionAuthMiddleware.AuthenticateAsync(ctx, svc);
            return Results.Ok(new AuthStatusResponse(await svc.NeedsSetupAsync(ct), user is not null, user?.Email, user?.IsAdmin));
        });

        auth.MapPost("/setup", async (SetupRequest req, HttpContext ctx, AuthService svc, CancellationToken ct) =>
        {
            var result = await svc.SetupAsync(req.Code, req.Email, req.Password, ct);
            if (result.Token is null) return Results.BadRequest(new { error = result.Error });
            SessionAuthMiddleware.IssueSessionCookie(ctx, result.Token);
            return Results.Ok();
        });

        auth.MapPost("/login", async (LoginRequest req, HttpContext ctx, AuthService svc, CancellationToken ct) =>
        {
            var result = await svc.LoginAsync(req.Email, req.Password, ct);
            if (result.Token is null) return Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
            SessionAuthMiddleware.IssueSessionCookie(ctx, result.Token);
            return Results.Ok();
        });

        auth.MapPost("/logout", async (HttpContext ctx, AuthService svc, CancellationToken ct) =>
        {
            if (ctx.Request.Cookies[SessionAuthMiddleware.SessionCookie] is { Length: > 0 } token)
                await svc.LogoutAsync(token, ct);
            SessionAuthMiddleware.ClearSessionCookie(ctx);
            return Results.NoContent();
        });
    }
}
