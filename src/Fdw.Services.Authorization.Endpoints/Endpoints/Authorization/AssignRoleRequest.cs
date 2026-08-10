using System;
using System.ComponentModel.DataAnnotations;
using Fdw.Services.Users.Endpoints;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request DTO for assigning a role to a user.
/// </summary>
public class AssignRoleRequest : UserScopedRequest
{
    /// <summary>
    /// Gets or sets the role name to assign.
    /// </summary>
    // Why: the client contract marks this Required; the server accepted an empty role name and
    // only failed later when the lookup found no such role.
    [Required]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional tenant ID for tenant-scoped role assignment.
    /// </summary>
    public Guid? TenantId { get; set; }
}
