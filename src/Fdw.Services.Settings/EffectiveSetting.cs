namespace Fdw.Services.Settings;

/// <summary>
/// What a setting is at one level of the tenant and organization tree, and where that value comes from (FDW-1579).
/// </summary>
/// <param name="Name">The setting's name.</param>
/// <param name="DataType">The setting's data type, as the platform defines it.</param>
/// <param name="Description">What the setting is for, or null when the platform gave none.</param>
/// <param name="Value">The value in force at the level asked for.</param>
/// <param name="SetAt">The level the value was set at: Platform, Tenant or Organization.</param>
/// <param name="LockedAt">The level that locked the setting against the levels below it, or null when no level did.</param>
public sealed record EffectiveSetting(string Name, string DataType, string? Description, string Value, string SetAt, string? LockedAt)
{
    /// <summary>Gets whether a level above the one that set the value locked it, so the levels below cannot set their own.</summary>
    public bool IsLocked => LockedAt is not null;
}
