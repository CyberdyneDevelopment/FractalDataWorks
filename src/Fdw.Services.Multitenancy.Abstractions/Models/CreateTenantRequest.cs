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
}
