namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request to set an organization's value for a setting (FDW-1579). An organization has no level below it, so it cannot lock.
/// </summary>
public class SetOrganizationInheritedSettingRequest : OrganizationInheritedSettingsRequest
{
    /// <summary>Gets or sets the setting.</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value.</summary>
    public string Value { get; set; } = string.Empty;
}
