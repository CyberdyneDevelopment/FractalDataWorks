using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Data transfer object for tenant information.
/// </summary>
public sealed class TenantDto
{
    /// <summary>Gets or sets the tenant unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant slug (URL-friendly identifier).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the tenant is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the connection name for tenant-specific database.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the tenant theme settings.</summary>
    public TenantThemeDto? Theme { get; set; }

    /// <summary>Gets or sets the available roles for this tenant.</summary>
    public IList<string> AvailableRoles { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether this is the user's default tenant.
    /// Populated by the list endpoint when the caller is authenticated.
    /// Always <c>false</c> for admin list responses where per-user default is not applicable.
    /// </summary>
    public bool IsDefault { get; set; }
}
