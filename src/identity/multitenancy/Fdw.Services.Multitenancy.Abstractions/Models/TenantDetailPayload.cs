using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Clients.Models;

/// <summary>
/// Represents detailed information about a tenant.
/// </summary>
public sealed class TenantDetailPayload
{
    /// <summary>
    /// Gets or sets the tenant unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant display name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the tenant slug (URL-friendly identifier).
    /// </summary>
    public string Slug { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the connection name for tenant-specific database.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the available roles for this tenant.
    /// </summary>
    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}
