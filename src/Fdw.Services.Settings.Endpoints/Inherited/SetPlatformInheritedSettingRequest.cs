namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request to set the platform's value for a setting, and to lock it against every tenant or leave it open (FDW-1579).
/// </summary>
public class SetPlatformInheritedSettingRequest
{
    /// <summary>Gets or sets the setting.</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets whether tenants may not set their own value.</summary>
    public bool Locked { get; set; }
}
