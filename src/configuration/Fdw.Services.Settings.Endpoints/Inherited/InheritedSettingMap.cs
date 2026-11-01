using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Maps an effective setting to its response (FDW-1579).
/// </summary>
internal static class InheritedSettingMap
{
    internal static InheritedSettingResponse Of(EffectiveSetting setting)
        => new()
        {
            Name = setting.Name,
            DataType = setting.DataType,
            Description = setting.Description,
            Value = setting.Value,
            SetAt = setting.SetAt,
            LockedAt = setting.LockedAt,
            IsLocked = setting.IsLocked,
        };

    internal static InheritedSettingsResponse Of(IReadOnlyList<EffectiveSetting> settings)
        => new() { Settings = [.. settings.Select(setting => Of(setting))] };
}
