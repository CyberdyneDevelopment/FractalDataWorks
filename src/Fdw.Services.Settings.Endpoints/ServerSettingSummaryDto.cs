using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Summary DTO for a server-level setting, used in list views.
/// </summary>
public sealed class ServerSettingSummaryDto
{
    /// <summary>Gets or sets the setting unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the setting name.</summary>
    public required string SettingName { get; set; }

    /// <summary>Alias for <see cref="SettingName"/> for generic API consumers expecting a 'name' field.</summary>
    public string Name => SettingName;

    /// <summary>Gets or sets the current value.</summary>
    public required string SettingValue { get; set; }

    /// <summary>Gets or sets the data type.</summary>
    public required string DataType { get; set; }

    /// <summary>Gets or sets whether the setting is active.</summary>
    public bool IsActive { get; set; }
}
