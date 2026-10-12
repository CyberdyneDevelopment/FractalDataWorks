using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request DTO for updating a role-level setting override.
/// </summary>
public sealed class UpdateRoleSettingRequest
{
    /// <summary>Gets or sets the tenant identifier (from route).</summary>
    [Required]
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the role name (from route).</summary>
    [Required]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the setting name (from route).</summary>
    [Required]
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the new overridden value.</summary>
    public string? SettingValue { get; set; }

    /// <summary>Gets or sets whether the override is active.</summary>
    public bool? IsActive { get; set; }
}
