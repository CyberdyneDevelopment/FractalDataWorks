using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>The default-value settings of the Pipelines share of FDW-3823: data sets, orchestration, ETL, mappers, pipelines and resiliency.</summary>
/// <remarks>Owned by fdw-services-fixes. Each setting is a name constant here, a definition in <see cref="PipelinesDefaults"/> and a row in the seed's
/// "FDW-3823 (Pipelines)" block.</remarks>
public static partial class DefaultValueSettings
{
    private static readonly Dictionary<string, SettingDefinition> PipelinesDefaults =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
        };
}
