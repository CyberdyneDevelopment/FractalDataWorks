namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// One setting as it stands at a level of the tenant and organization tree (FDW-1579).
/// </summary>
public class InheritedSettingResponse
{
    /// <summary>Gets or sets the setting's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the setting's data type.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets what the setting is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the value in force.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the level the value was set at: Platform, Tenant or Organization.</summary>
    public string SetAt { get; set; } = string.Empty;

    /// <summary>Gets or sets the level that locked the setting against the levels below it, or null when none did.</summary>
    public string? LockedAt { get; set; }

    /// <summary>Gets or sets whether a level has locked the setting.</summary>
    public bool IsLocked { get; set; }
}
