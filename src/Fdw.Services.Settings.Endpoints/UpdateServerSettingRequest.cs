using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request DTO for updating a server-level setting.
/// </summary>
public sealed class UpdateServerSettingRequest
{
    /// <summary>Gets or sets the setting name (from route).</summary>
    [Required]
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the new setting value.</summary>
    public string? SettingValue { get; set; }

    /// <summary>Gets or sets the new description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new minimum value.</summary>
    public string? MinValue { get; set; }

    /// <summary>Gets or sets the new maximum value.</summary>
    public string? MaxValue { get; set; }

    /// <summary>Gets or sets whether the setting is active.</summary>
    public bool? IsActive { get; set; }
}
