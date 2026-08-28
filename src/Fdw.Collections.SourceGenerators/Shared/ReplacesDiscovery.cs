using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Shared logic for discovering [Replaces] attributes and building replacement maps.
/// </summary>
/// <remarks>
/// Why: Centralizes replacement resolution so TypeCollectionGenerator, MutableTypeCollectionGenerator,
/// and ServiceTypeCollectionGenerator all use the same logic. Handles chain resolution (A replaces B,
/// B replaces C => A is the final replacement for C) and conflict detection.
/// </remarks>
internal static class ReplacesDiscovery
{
    private const string ReplacesAttributeName = "Fdw.Collections.Attributes.ReplacesAttribute";

    /// <summary>
    /// Scans all types with [Replaces] in the compilation and builds a replacement map.
    /// Returns a dictionary mapping replaced type full name to the replacement type full name.
    /// Reports diagnostics for conflicts (multiple replacements for the same target).
    /// </summary>
    public static Dictionary<string, string> BuildReplacementMap(
        Compilation compilation,
        SourceProductionContext context)
    {
        var replacesAttrType = compilation.GetTypeByMetadataName(ReplacesAttributeName);
        if (replacesAttrType == null)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var rawReplacements = new List<(string ReplacementFullName, string OriginalFullName)>();

        // Scan current assembly
        ScanNamespace(compilation.Assembly.GlobalNamespace, replacesAttrType, rawReplacements);

        // Scan referenced assemblies
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            ScanNamespace(assembly.GlobalNamespace, replacesAttrType, rawReplacements);
        }

        // Detect conflicts: multiple types replacing the same original
        var byOriginal = rawReplacements
            .GroupBy(r => r.OriginalFullName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var replacementMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var kvp in byOriginal)
        {
            if (kvp.Value.Count > 1)
            {
                var replacers = string.Join(", ", kvp.Value.Select(r => r.ReplacementFullName));
                context.ReportDiagnostic(Diagnostic.Create(
                    TypeCollectionGeneratorDiagnostics.DuplicateReplacesTarget,
                    Location.None,
                    kvp.Key,
                    replacers));
            }
            else
            {
                replacementMap[kvp.Key] = kvp.Value[0].ReplacementFullName;
            }
        }

        ResolveChains(replacementMap);

        return replacementMap;
    }

    /// <summary>
    /// Resolves replacement chains so that transitive replacements work correctly.
    /// If A replaces B and B replaces C, the map is updated so C maps to A.
    /// </summary>
    private static void ResolveChains(Dictionary<string, string> map)
    {
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var key in map.Keys.ToList())
        {
            var current = key;
            var visited = new HashSet<string>(StringComparer.Ordinal) { current };

            while (map.TryGetValue(current, out var next))
            {
                if (map.ContainsKey(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    current = next;
                }
                else
                {
                    break;
                }
            }

            // Map every visited node (except the terminal) to the terminal replacement
            var terminal = map[current];
            foreach (var node in visited)
            {
                if (map.ContainsKey(node))
                {
                    resolved[node] = terminal;
                }
            }
        }

        foreach (var kvp in resolved)
        {
            map[kvp.Key] = kvp.Value;
        }
    }

    private static void ScanNamespace(
        INamespaceSymbol ns,
        INamedTypeSymbol replacesAttrType,
        List<(string ReplacementFullName, string OriginalFullName)> results)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            ScanType(type, replacesAttrType, results);
        }

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            ScanNamespace(nestedNs, replacesAttrType, results);
        }
    }

    private static void ScanType(
        INamedTypeSymbol type,
        INamedTypeSymbol replacesAttrType,
        List<(string ReplacementFullName, string OriginalFullName)> results)
    {
        foreach (var attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, replacesAttrType))
                continue;

            if (attr.ConstructorArguments.Length < 1)
                continue;

            var originalType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            if (originalType == null)
                continue;

            results.Add((type.ToDisplayString(), originalType.ToDisplayString()));
        }

        foreach (var nested in type.GetTypeMembers())
        {
            ScanType(nested, replacesAttrType, results);
        }
    }

    /// <summary>
    /// Filters a list of TypeOptionModels by removing replaced types and optionally warning about
    /// missing originals.
    /// </summary>
    public static ImmutableArray<TypeOptionModel> FilterReplacedTypeOptions(
        ImmutableArray<TypeOptionModel> options,
        Dictionary<string, string> replacementMap,
        SourceProductionContext context)
    {
        if (replacementMap.Count == 0)
            return options;

        var optionsByFullName = new HashSet<string>(
            options.Select(o => o.FullTypeName),
            StringComparer.Ordinal);

        foreach (var kvp in replacementMap)
        {
            if (!optionsByFullName.Contains(kvp.Key) && optionsByFullName.Contains(kvp.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    TypeCollectionGeneratorDiagnostics.ReplacedTypeNotFound,
                    Location.None,
                    kvp.Value,
                    kvp.Key));
            }
        }

        return options
            .Where(o => !replacementMap.ContainsKey(o.FullTypeName))
            .ToImmutableArray();
    }

    /// <summary>
    /// Filters a list of ServiceTypeOptionModels by removing replaced types.
    /// </summary>
    public static ImmutableArray<ServiceTypeOptionModel> FilterReplacedServiceTypeOptions(
        ImmutableArray<ServiceTypeOptionModel> options,
        Dictionary<string, string> replacementMap,
        SourceProductionContext context)
    {
        if (replacementMap.Count == 0)
            return options;

        var optionsByFullName = new HashSet<string>(
            options.Select(o => o.FullTypeName),
            StringComparer.Ordinal);

        foreach (var kvp in replacementMap)
        {
            if (!optionsByFullName.Contains(kvp.Key) && optionsByFullName.Contains(kvp.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    TypeCollectionGeneratorDiagnostics.ReplacedTypeNotFound,
                    Location.None,
                    kvp.Value,
                    kvp.Key));
            }
        }

        return options
            .Where(o => !replacementMap.ContainsKey(o.FullTypeName))
            .ToImmutableArray();
    }
}
