namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

/// <summary>
/// Current user identity and permissions, populated by UserContext middleware from auth (e.g. claims).
/// Do not take user id, role, or admin flag from the client; use this context instead.
/// </summary>
public interface IUserContext
{
    /// <summary>Current user's ID, or null if unauthenticated.</summary>
    Guid? UserId { get; }

    /// <summary>Current user's display name.</summary>
    string Username { get; }

    /// <summary>User role (e.g. "Admin", "User"). Used for permission checks.</summary>
    string Role { get; }

    /// <summary>Fine-grained rights (e.g. "EditStandardSearch", "DeleteAnyRequest").</summary>
    IReadOnlyList<string> Rights { get; }

    /// <summary>True when Role is "Admin" or Rights contains "Admin". Use for admin-only operations.</summary>
    bool IsAdmin { get; }
}
