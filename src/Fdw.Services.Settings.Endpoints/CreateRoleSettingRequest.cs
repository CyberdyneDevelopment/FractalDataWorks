using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request DTO for creating a role-level setting override.
/// </summary>
public sealed class CreateRoleSettingRequest
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the role name.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the setting name (must match a server setting).</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the overridden value for this role.</summary>
    [Required]
    public string SettingValue { get; set; } = string.Empty;
}
