using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request to update a role.
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// Gets or sets the role name (bound from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name, if updating.
    /// </summary>
    // Why: matches the client contract and authz.Role.DisplayName nvarchar(200). Without it the
    // server accepted an over-long value that then failed at the database.
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description, if updating.
    /// </summary>
    // Why: authz.Role.Description is nvarchar(500).
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parent role name for inheritance, if updating.
    /// </summary>
    public string? ParentRoleName { get; set; }

    /// <summary>
    /// Gets or sets the sort order, if updating.
    /// </summary>
    public int? SortOrder { get; set; }
}
