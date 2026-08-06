using System;
using System.Collections.Immutable;
using System.Linq;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators.Generators;

/// <summary>
/// Utility helpers for configuration source generation.
/// </summary>
/// <remarks>
/// Why: Wave C5 removes IConfigurationType, ConfigurationTypeBase, and the ConfigurationTypes
/// TypeCollection. The Generate() method that emitted ConfigurationType subclasses is deleted.
/// DetermineTargetNamespace() is retained because TypeCollectionDdlGenerator uses it to pick
/// the namespace for the generated TypeCollectionDdlRegistry class.
/// </remarks>
public static class ConfigurationTypesGenerator
{
    /// <summary>
    /// Determines the target namespace for registry classes.
    /// Uses the most common namespace prefix among configurations.
    /// </summary>
    public static string DetermineTargetNamespace(ImmutableArray<ConfigurationModel> configs)
    {
        if (configs.IsDefaultOrEmpty)
            return "Fdw.Configuration";

        // Find common namespace prefix
        var namespaces = configs.Select(c => c.Namespace).Distinct(StringComparer.Ordinal).ToList();

        if (namespaces.Count == 1)
            return namespaces[0];

        // Find common prefix
        var first = namespaces[0];
        var prefixLength = first.Length;

        foreach (var s in namespaces.Skip(1))
        {
            while (prefixLength > 0 && !s.StartsWith(first.Substring(0, prefixLength), StringComparison.Ordinal))
            {
                prefixLength--;
            }
        }

        var commonPrefix = first.Substring(0, prefixLength);
        if (!string.IsNullOrEmpty(commonPrefix))
        {
            // Remove trailing dot
            if (commonPrefix.EndsWith(".", StringComparison.Ordinal))
                commonPrefix = commonPrefix.Substring(0, commonPrefix.Length - 1);
            return commonPrefix;
        }

        // Default to first namespace
        return namespaces[0];
    }
}
