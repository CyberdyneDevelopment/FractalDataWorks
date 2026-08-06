using System;
using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Configuration for a tenant, bindable from appsettings.json.
/// Add new tenants by adding sections under "Tenants" in configuration.
/// </summary>
/// <example>
/// {
///   "Tenants": {
///     "acme": {
///       "Id": "550e8400-e29b-41d4-a716-446655440000",
///       "Name": "Acme Corporation",
///       "Slug": "acme",
///       "IsActive": true,
///       "ConnectionName": "AcmeDb",
///       "Theme": { "PrimaryColor": "#ff5722" },
///       "Options": { "MaxUsers": 100 },
///       "AvailableRoles": ["Admin", "User"]
///     }
///   }
/// }
/// </example>
public sealed class TenantConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the tenant's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the service type (domain) - always "Tenant" for this configuration.
    /// </summary>
    public string ServiceType => "Tenant";

    /// <summary>
    /// Gets the service option type. Tenants don't have subtypes.
    /// </summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the tenant's slug for URLs.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the organization prefix applied to permission policy names on the API
    /// surface (e.g. "acme" → "acme:connections:read"). Null/empty means no prefix.
    /// </summary>
    public string? OrgPrefix { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this tenant is the global/home tenant.
    /// Resolved from <c>tenant.Tenants.IsGlobal</c>; never hardcoded.
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Gets or sets the connection name for tenant isolation.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the theme configuration.
    /// </summary>
    public TenantThemeConfiguration Theme { get; set; } = new();

    /// <summary>
    /// Gets or sets the options configuration.
    /// </summary>
    public TenantOptionsConfiguration Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the available roles for this tenant.
    /// Populated from the tenant record in ConfigurationDb; no default values are applied here.
    /// </summary>
    public ICollection<string> AvailableRoles { get; set; } = new List<string>();

    /// <inheritdoc/>
    public string SectionName => $"Tenants:{Slug}";

}
