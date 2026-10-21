using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Multitenancy.Clients.Models;

/// <summary>
/// Data transfer object for creating a new tenant.
/// </summary>
public sealed class CreateTenantRequest
{
    /// <summary>
    /// Gets or sets the tenant display name.
    /// </summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the tenant slug (URL-friendly identifier).
    /// </summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string Slug { get; set; } = "";

    /// <summary>
    /// Gets or sets the connection name for tenant-specific database.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the prefix applied to permission policy names for the tenant (for example "acme"); null for none.
    /// </summary>
    [StringLength(32)]
    public string? OrgPrefix { get; set; }

    /// <summary>
    /// Gets or sets the person who becomes the tenant's first administrator: a user id or a user name. They must already have an account.
    /// A tenant is never made without one (FDW-1578).
    /// </summary>
    [Required, StringLength(256, MinimumLength = 1)]
    public string FirstAdministrator { get; set; } = "";

    /// <summary>
    /// Gets or sets the name of the role the first administrator is given in the new tenant. The caller must hold every permission the role
    /// carries.
    /// </summary>
    [Required, StringLength(256, MinimumLength = 1)]
    public string AdministratorRole { get; set; } = "";
}
