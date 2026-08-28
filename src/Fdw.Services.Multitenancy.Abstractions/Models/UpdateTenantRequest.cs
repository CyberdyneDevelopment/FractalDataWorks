using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Multitenancy.Clients.Models;

/// <summary>
/// Data transfer object for updating an existing tenant.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class UpdateTenantRequest
{
    /// <summary>
    /// Gets or sets the updated tenant display name.
    /// </summary>
    [StringLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tenant is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets the updated connection name.
    /// </summary>
    public string? ConnectionName { get; set; }
}
