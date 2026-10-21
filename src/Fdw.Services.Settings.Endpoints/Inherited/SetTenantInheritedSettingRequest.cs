namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request to set a tenant's value for a setting, and to lock it against the organizations of the tenant or leave it open (FDW-1579).
/// </summary>
public class SetTenantInheritedSettingRequest : TenantInheritedSettingKeyRequest
{
    /// <summary>Gets or sets the value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the organizations of the tenant may not set their own value.</summary>
    public bool Locked { get; set; }
}
