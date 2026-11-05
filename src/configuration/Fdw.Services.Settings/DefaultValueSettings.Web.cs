using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>The default-value settings of the Web share of FDW-3823: REST endpoints and the TUI components.</summary>
/// <remarks>Owned by fdw-documents. Each setting is a name constant here, a definition in <see cref="WebDefaults"/> and a row in the seed's
/// "FDW-3823 (Web)" block.</remarks>
public static partial class DefaultValueSettings
{
    private static readonly Dictionary<string, SettingDefinition> WebDefaults =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
        };
}
