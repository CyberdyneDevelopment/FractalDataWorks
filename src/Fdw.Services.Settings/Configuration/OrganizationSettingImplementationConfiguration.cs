using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Settings.Configuration;

/// <summary>
/// Database-backed configuration for organization-level setting values (FDW-1579).
/// Generates the table <c>settings.OrganizationSetting</c>.
/// </summary>
/// <remarks>
/// An organization setting is the value one organization holds for a setting the platform defines. The effective value is the nearest one
/// set: Organization, then Tenant, then Platform, unless a level above locked the setting.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Settings",
    ServiceType = "OrganizationSetting")]
public sealed partial class OrganizationSettingImplementationConfiguration
    : IOrganizationSettingImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the unique identifier for this organization setting.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant the organization belongs to; it scopes who may read the row.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the organization this setting belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the setting name (must match a ServerSetting.SettingName).
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value this organization holds for the setting.
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this setting value is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

}
