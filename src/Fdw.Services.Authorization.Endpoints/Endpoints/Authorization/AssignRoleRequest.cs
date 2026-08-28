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
    [Required]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional tenant ID for tenant-scoped role assignment.
    /// </summary>
    public Guid? TenantId { get; set; }
}
