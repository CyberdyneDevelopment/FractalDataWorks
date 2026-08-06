using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Summary DTO for a role-level setting override, used in list views.
/// </summary>
public sealed class RoleSettingSummaryDto
{
    /// <summary>Gets or sets the setting unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the role name.</summary>
    public required string RoleName { get; set; }

    /// <summary>Gets or sets the setting name.</summary>
    public required string SettingName { get; set; }

    /// <summary>Gets or sets the overridden value.</summary>
    public required string SettingValue { get; set; }

    /// <summary>Gets or sets whether the setting override is active.</summary>
    public bool IsActive { get; set; }
}
