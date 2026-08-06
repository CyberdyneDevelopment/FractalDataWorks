using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request DTO for creating a tenant-level setting override.
/// </summary>
public sealed class CreateTenantSettingRequest
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the setting name (must match a server setting).</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the overridden value for this tenant.</summary>
    [Required]
    public string SettingValue { get; set; } = string.Empty;
}
