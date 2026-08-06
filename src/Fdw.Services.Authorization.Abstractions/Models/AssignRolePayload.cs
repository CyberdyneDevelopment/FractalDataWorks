using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Data transfer object for assigning a role to a user.
/// </summary>
public sealed class AssignRolePayload
{
    /// <summary>
    /// Gets or sets the role name to assign.
    /// </summary>
    // Why: kept in step with the server contract and the Users client copy, both of which
    // require a role name.
    [Required]
    public string RoleName { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional tenant ID for tenant-scoped role assignment.
    /// </summary>
    public Guid? TenantId { get; set; }
}
