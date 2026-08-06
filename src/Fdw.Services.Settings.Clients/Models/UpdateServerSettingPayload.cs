namespace Fdw.Services.Settings.Clients.Models;

/// <summary>
/// Request to update a server setting value.
/// </summary>
public sealed class UpdateServerSettingPayload
{
    /// <summary>
    /// Gets or sets the setting name.
    /// </summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new setting value.
    /// </summary>
    public string? SettingValue { get; set; }

    // Why: the server endpoint accepts these four optional fields (gated `if (req.X is not null)`),
    // but the client payload previously omitted them, so a metadata edit was impossible to send. Carrying
    // them here lets the Update client method ship them without dropping the values silently.

    /// <summary>
    /// Gets or sets the new description. When null the server leaves the existing value unchanged.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the new minimum value. When null the server leaves the existing value unchanged.
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the new maximum value. When null the server leaves the existing value unchanged.
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets whether the setting is active. When null the server leaves the existing value unchanged.
    /// </summary>
    public bool? IsActive { get; set; }
}
