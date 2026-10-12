using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request DTO for creating a new server-level setting.
/// </summary>
public sealed class CreateServerSettingRequest
{
    /// <summary>Gets or sets the setting name (unique key).</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the setting value.</summary>
    [Required]
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type (e.g., "String", "Int32", "Boolean", "Decimal").</summary>
    [Required]
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the minimum allowed value for numeric settings.</summary>
    public string? MinValue { get; set; }

    /// <summary>Gets or sets the maximum allowed value for numeric settings.</summary>
    public string? MaxValue { get; set; }
}
