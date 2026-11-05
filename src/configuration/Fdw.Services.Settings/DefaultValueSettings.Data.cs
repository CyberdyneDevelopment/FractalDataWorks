using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>The default-value settings of the Data share of FDW-3823: commands, data, row sources, importers, transformations, DDL and data stores.</summary>
/// <remarks>Owned by fdw-commands-guard. Each setting is a name constant here, a definition in <see cref="DataDefaults"/> and a row in the seed's
/// "FDW-3823 (Data)" block.</remarks>
public static partial class DefaultValueSettings
{
    private static readonly Dictionary<string, SettingDefinition> DataDefaults =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
        };
}
