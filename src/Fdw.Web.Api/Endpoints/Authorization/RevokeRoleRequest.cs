using Fdw.Services.Users.Endpoints;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request DTO for revoking a role from a user.
/// </summary>
public class RevokeRoleRequest : UserScopedRequest
{
    /// <summary>
    /// Gets or sets the role name to revoke (bound from route).
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
}
