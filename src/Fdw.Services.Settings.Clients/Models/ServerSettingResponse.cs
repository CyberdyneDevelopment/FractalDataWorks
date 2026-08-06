namespace Fdw.Services.Settings.Clients.Models;

using System;

/// <summary>
/// Represents a server setting returned from the Settings API.
/// </summary>
public sealed class ServerSettingResponse
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the setting name.
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public string SettingValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the setting value.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the setting is active.
    /// </summary>
    public bool IsActive { get; set; }
}
