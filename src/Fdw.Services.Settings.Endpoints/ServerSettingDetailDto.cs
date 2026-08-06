using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Detailed DTO for a server-level setting, including min/max bounds and description.
/// </summary>
public sealed class ServerSettingDetailDto
{
    /// <summary>Gets or sets the setting unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the setting name.</summary>
    public required string SettingName { get; set; }

    /// <summary>Gets or sets the current value.</summary>
    public required string SettingValue { get; set; }

    /// <summary>Gets or sets the data type.</summary>
    public required string DataType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the minimum allowed value for numeric settings.</summary>
    public string? MinValue { get; set; }

    /// <summary>Gets or sets the maximum allowed value for numeric settings.</summary>
    public string? MaxValue { get; set; }

    /// <summary>Gets or sets whether the setting is active.</summary>
    public bool IsActive { get; set; }
}
