using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request to create a new role.
/// </summary>
// Why the DataAnnotations duplicate CreateRoleRequestValidator: OpenAPI/Swagger generates the
// published schema from DataAnnotations, not from FluentValidation. Without them this type
// advertised no constraints at all to API consumers even though the validator rejected the input.
// The two must be kept in step -- ContractParityTests asserts it.
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
