using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Clients.Models;

/// <summary>
/// Data transfer object for creating a new role.
/// </summary>
public sealed class CreateRolePayload
{
    /// <summary>
    /// Gets or sets the unique name for the new role.
    /// </summary>
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the display name for the new role.
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description for the new role.
    /// </summary>
    // Why: authz.Role.Description is nvarchar(500). Both sides previously allowed 1000, so a
    // 501-1000 character description passed validation and then failed at the database.
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the name of the parent role for the new role.
    /// </summary>
    public string? ParentRoleName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the new role is scoped to a tenant.
    /// </summary>
    public bool IsTenantScoped { get; set; }
}
