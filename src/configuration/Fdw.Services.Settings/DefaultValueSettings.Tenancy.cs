using System;
using System.Collections.Generic;

namespace Fdw.Services.Settings;

/// <summary>The default-value settings of the Tenancy share of FDW-3823: multitenancy, authorization, tokens, users, settings, notifications, quality, calculations, operations and the MCP bus.</summary>
/// <remarks>Owned by fdw-services:FDW-3668. Each setting is a name constant here, a definition in <see cref="TenancyDefaults"/> and a row in the seed's
/// "FDW-3823 (Tenancy)" block.</remarks>
public static partial class DefaultValueSettings
{
    private static readonly Dictionary<string, SettingDefinition> TenancyDefaults =
        new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
        {
        };
}
