using System.Security.Claims;
using KPMG.QualitativeBenchmarking.Application.Abstraction;

namespace KPMG.QualitativeBenchmarking.Api.Middleware;

/// <summary>
/// Populates the current user context for the request. Replace this with your real auth (e.g. JWT)
/// and set <see cref="UserContextData"/> from claims instead of headers.
/// </summary>
public sealed class UserContextMiddleware
{
    public const string HttpContextKey = "UserContext";

    /// <summary>Header names used when auth is not yet implemented (placeholder). Set from your auth later.</summary>
    public static class HeaderNames
    {
        public const string UserId = "X-User-Id";
        public const string Username = "X-Username";
        public const string Role = "X-Role";
        public const string Rights = "X-Rights"; // comma-separated
    }

    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var data = ResolveUserContext(context);
        context.Items[HttpContextKey] = data;
        await _next(context);
    }

    /// <summary>
    /// Resolve user from request. Default: read from headers (dev placeholder).
    /// Replace with: context.User (ClaimsPrincipal) from your auth middleware.
    /// </summary>
    private static UserContextData ResolveUserContext(HttpContext context)
    {
        // Prefer claims if the user is authenticated (e.g. after you add JWT middleware).
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User.FindFirst("name")?.Value;
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                ?? context.User.FindFirst("role")?.Value;
            var rights = context.User.FindFirst("rights")?.Value?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new List<string>();

            if (Guid.TryParse(userId, out var id))
                return new UserContextData(id, name ?? "", role ?? "User", rights);
        }

        // Placeholder: read from headers (for development or until you wire your auth).
        var headerUserId = context.Request.Headers[HeaderNames.UserId].FirstOrDefault();
        var headerUsername = context.Request.Headers[HeaderNames.Username].FirstOrDefault() ?? "";
        var headerRole = context.Request.Headers[HeaderNames.Role].FirstOrDefault() ?? "User";
        var headerRights = context.Request.Headers[HeaderNames.Rights].FirstOrDefault()?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? new List<string>();

        if (Guid.TryParse(headerUserId, out var parsedId))
            return new UserContextData(parsedId, headerUsername, headerRole, headerRights);

        return new UserContextData(null, "", "User", new List<string>());
    }
}

/// <summary>Data stored in HttpContext.Items by the middleware. Your auth can set this from claims.</summary>
public sealed class UserContextData
{
    public Guid? UserId { get; }
    public string Username { get; }
    public string Role { get; }
    public IReadOnlyList<string> Rights { get; }

    public UserContextData(Guid? userId, string username, string role, IReadOnlyList<string> rights)
    {
        UserId = userId;
        Username = username ?? "";
        Role = role ?? "User";
        Rights = rights ?? new List<string>();
    }
}
