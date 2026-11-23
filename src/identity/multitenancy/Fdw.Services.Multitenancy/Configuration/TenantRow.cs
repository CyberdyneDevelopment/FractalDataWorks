using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy.Configuration;

/// <summary>
/// A row of <c>tenant.Tenants</c> as the single-tenant option's tenant lookup reads it (FDW-6528): the columns that say which tenant it is, whether it is active and
/// whether it is the global one. The theme, options and roles of a tenant are not read: a lookup on a host that resolves no tenant per request does not use them.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class TenantRow
{
    /// <summary>Gets or sets the tenant's durable Id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant's slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the prefix of the tenant's organizations; null when it has none.</summary>
    public string? OrgPrefix { get; set; }

    /// <summary>Gets or sets the tenant's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the tenant is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the global tenant, whose members may act as any tenant.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Gets or sets a value indicating whether a UI offers the tenant to switch to.</summary>
    public bool IsSelectable { get; set; }

    /// <summary>Gets or sets the connection the tenant's own data lives on; null when it has none.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the tenant; only the current version is read.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets a value indicating whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
