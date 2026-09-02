using System.Security.Cryptography;
using Mochi.Application.Auth;
using Mochi.Domain.Accounts;

namespace Mochi.Api.Auth;

/// <summary>
/// Session and CSRF enforcement for the dashboard API (ADR 0004). Exempt:
/// /api/collect (public by design) and /api/auth (login is how you get a
/// session). Static files and the SPA shell pass through untouched.
/// </summary>
public sealed class SessionAuthMiddleware(RequestDelegate next)
{
    private const string UserItem = "mochi.user";

    /// <summary>Session cookie name.</summary>
    public const string SessionCookie = "mochi_session";

    /// <summary>Readable XSRF cookie; Angular mirrors it into the header.</summary>
    public const string XsrfCookie = "XSRF-TOKEN";

    /// <summary>Header the client must echo on non-GET API calls.</summary>
    public const string XsrfHeader = "X-XSRF-TOKEN";

    /// <summary>Runs the checks, then the rest of the pipeline.</summary>
    public async Task InvokeAsync(HttpContext ctx, AuthService auth)
    {
        if (!ctx.Request.Cookies.ContainsKey(XsrfCookie))
        {
            ctx.Response.Cookies.Append(XsrfCookie, Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)),
                new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax, Secure = ctx.Request.IsHttps, Path = "/" });
        }

        var path = ctx.Request.Path;
        var isApi = path.StartsWithSegments("/api");
        var isCollect = path.StartsWithSegments("/api/collect");
        var isAuth = path.StartsWithSegments("/api/auth");

        if (isApi && !isCollect && !HttpMethods.IsGet(ctx.Request.Method))
        {
            var cookie = ctx.Request.Cookies[XsrfCookie];
            if (cookie is null || ctx.Request.Headers[XsrfHeader].ToString() != cookie)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsync("missing or mismatched XSRF token");
                return;
            }
        }

        if (isApi && !isCollect && !isAuth)
        {
            var user = await AuthenticateAsync(ctx, auth);
            if (user is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            ctx.Items[UserItem] = user;
        }

        await next(ctx);
    }

    /// <summary>Resolves the session cookie to a user, or null. Also used by /api/auth/status.</summary>
    public static async Task<User?> AuthenticateAsync(HttpContext ctx, AuthService auth)
        => ctx.Request.Cookies[SessionCookie] is { Length: > 0 } token
            ? await auth.AuthenticateAsync(token, ctx.RequestAborted)
            : null;

    /// <summary>The authenticated user set by the middleware. Null on exempt routes.</summary>
    public static User? CurrentUser(HttpContext ctx) => ctx.Items[UserItem] as User;

    /// <summary>Sets the session cookie after setup or login.</summary>
    public static void IssueSessionCookie(HttpContext ctx, string token)
        => ctx.Response.Cookies.Append(SessionCookie, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = ctx.Request.IsHttps,
            Path = "/",
            MaxAge = Session.AbsoluteLifetime,
        });

    /// <summary>Clears the session cookie on logout.</summary>
    public static void ClearSessionCookie(HttpContext ctx)
        => ctx.Response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/" });
}
