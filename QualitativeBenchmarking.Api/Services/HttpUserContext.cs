using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Api.Middleware;

namespace KPMG.QualitativeBenchmarking.Api.Services;

/// <summary>
/// Scoped implementation of <see cref="IUserContext"/> that reads from the current request's
/// HttpContext (populated by <see cref="UserContextMiddleware"/>).
/// </summary>
public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid? UserId => Data.UserId;
    public string Username => Data.Username;
    public string Role => Data.Role;
    public IReadOnlyList<string> Rights => Data.Rights;

    public bool IsAdmin =>
        string.Equals(Data.Role, "Admin", StringComparison.OrdinalIgnoreCase)
        || Data.Rights.Contains("Admin", StringComparer.OrdinalIgnoreCase);

    private UserContextData Data
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue(UserContextMiddleware.HttpContextKey, out var value) == true
                && value is UserContextData data)
                return data;
            return new UserContextData(null, "", "User", new List<string>());
        }
    }
}
