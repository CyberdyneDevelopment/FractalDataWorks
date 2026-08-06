using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request DTO that identifies a setting by name.
/// </summary>
public sealed class SettingNameRequest
{
    /// <summary>Gets or sets the setting name (from route).</summary>
    [Required]
    public string SettingName { get; set; } = string.Empty;
}
