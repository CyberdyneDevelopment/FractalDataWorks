using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>The default-value settings of the Themes share of FDW-3823: UI themes and the secret managers.</summary>
/// <remarks>
/// Owned by fdw-collections-guard, and empty: a secret manager's tuning defaults are carried by its service type option
/// (<c>IDeclaresDefaults</c>), its other fields are the caller's choice, and a new theme takes what it leaves out from the default theme row.
/// </remarks>
public static partial class DefaultValueSettings
{
    private static readonly Dictionary<string, SettingDefinition> ThemesDefaults =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
        };
}
