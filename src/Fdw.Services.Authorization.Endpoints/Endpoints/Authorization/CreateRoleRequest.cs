using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request to create a new role.
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parent role name for inheritance.
    /// </summary>
    public string? ParentRoleName { get; set; }

    /// <summary>
    /// Gets or sets whether this role is tenant-scoped.
    /// </summary>
    public bool IsTenantScoped { get; set; }
}
