using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Data transfer object for updating an existing role.
/// </summary>
// Why: pure request payload, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class UpdateRolePayload
{
    /// <summary>
    /// Gets or sets the updated display name for the role.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the updated description for the role.
    /// </summary>
    // Why: authz.Role.Description is nvarchar(500) -- see CreateRolePayload.
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated parent role name for the role.
    /// </summary>
    public string? ParentRoleName { get; set; }
}
