using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Fdw.Configuration;

/// <summary>
/// Generic multi-tenant configuration provider that flattens hierarchical configuration data.
/// Works with any IConfigurationSource that provides hierarchical data.
/// </summary>
public class FdwConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationSource _source;
    private readonly Func<IDictionary<int, IDictionary<string, object>>> _loadHierarchy;
    private readonly string _sectionName;

    /// <summary>
    /// Initializes a new instance of the provider.
    /// </summary>
    /// <param name="source">The configuration source.</param>
    /// <param name="loadHierarchy">Function to load hierarchical data from the source.</param>
    /// <param name="sectionName">Optional section name prefix for configuration keys.</param>
    public FdwConfigurationProvider(
        IConfigurationSource source,
        Func<IDictionary<int, IDictionary<string, object>>> loadHierarchy,
        string sectionName = "")
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _loadHierarchy = loadHierarchy ?? throw new ArgumentNullException(nameof(loadHierarchy));
        _sectionName = sectionName ?? string.Empty;
    }

    /// <summary>
    /// Loads configuration data by calling the source's LoadHierarchy function
    /// and flattening the result.
    /// </summary>
    public override void Load()
    {
        var hierarchy = _loadHierarchy();
        Data = FlattenHierarchy(hierarchy);
    }

    /// <summary>
    /// Flattens hierarchical configuration into a single dictionary.
    /// Merges levels in order: DEFAULT (0) → APPLICATION (1) → TENANT (2) → USER (3).
    /// Higher levels override lower levels.
    /// </summary>
    private Dictionary<string, string?> FlattenHierarchy(
        IDictionary<int, IDictionary<string, object>> hierarchy)
    {
        var flattened = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Merge in order: DEFAULT (0) → APPLICATION (1) → TENANT (2) → USER (3)
        for (int level = 0; level < 4; level++)
        {
            if (hierarchy.TryGetValue(level, out var levelData))
            {
                foreach (var kvp in levelData)
                {
                    // Skip hierarchy metadata columns
                    if (IsMetadataColumn(kvp.Key))
                    {
                        continue;
                    }

                    // Add with optional section prefix
                    var key = string.IsNullOrEmpty(_sectionName)
                        ? kvp.Key
                        : $"{_sectionName}:{kvp.Key}";

                    flattened[key] = kvp.Value?.ToString() ?? string.Empty;
                }
            }
        }

        return flattened;
    }

    /// <summary>
    /// Determines if a column is metadata that should be excluded from configuration.
    /// </summary>
    private static bool IsMetadataColumn(string columnName)
    {
        return columnName switch
        {
            "Id" or "Level" or "TenantId" or "UserId" or "CreatedAt" or "ModifiedAt" => true,
            _ => false
        };
    }
}
