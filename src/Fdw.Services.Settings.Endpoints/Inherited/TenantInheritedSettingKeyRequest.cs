namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request naming one setting of a tenant (FDW-1579).
/// </summary>
public class TenantInheritedSettingKeyRequest : TenantInheritedSettingsRequest
{
    /// <summary>Gets or sets the setting.</summary>
    public string SettingName { get; set; } = string.Empty;
}
