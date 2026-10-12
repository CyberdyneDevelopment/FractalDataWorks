using System.ComponentModel.DataAnnotations;
using Fdw.Services.Users.Endpoints;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request DTO for assigning a role to a user, in the tenant the caller is acting in.
/// </summary>
/// <remarks>
/// Carries no tenant: the role is granted in the caller's token tenant (FDW-825). To grant a role in
/// another tenant, a caller switches into it first.
/// </remarks>
public class AssignRoleRequest : UserScopedRequest
{
    /// <summary>
    /// Gets or sets the role name to assign.
    /// </summary>
    [Required]
    public string RoleName { get; set; } = string.Empty;
}
