using System.Collections.Generic;
using System.Collections.Immutable;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Analyzes parent/child relationships between configurations.
/// </summary>
/// <remarks>
/// Why: Parent-child structure was removed from [ManagedConfiguration] in FDW-395 Phase 6.
/// IDataNode now owns schema/table/parent-relationship metadata. All configuration types
/// are treated as flat root tables by the source generator. This analyzer is preserved for
/// structural compatibility but produces no relationships.
/// </remarks>
public static class ParentChildAnalyzer
{
    /// <summary>
    /// Builds a parent/child relationship graph from configuration models.
    /// </summary>
    public static ParentChildGraph BuildGraph(ImmutableArray<ConfigurationModel> configs)
    {
        var graph = new ParentChildGraph();

        // Register all configurations as flat roots — no parent-child relationships
        foreach (var config in configs)
        {
            graph.AddConfiguration(config);
        }

        return graph;
    }

    /// <summary>
    /// Gets all root configurations (those without a parent).
    /// </summary>
    /// <remarks>
    /// Why: All configurations are roots now that parent-child is owned by IDataNode.
    /// </remarks>
    public static IEnumerable<ConfigurationModel> GetRoots(ImmutableArray<ConfigurationModel> configs)
    {
        return configs;
    }

    /// <summary>
    /// Gets child configurations for a given parent.
    /// </summary>
    /// <remarks>
    /// Why: Always returns empty — parent-child structure is owned by IDataNode, not [ManagedConfiguration].
    /// </remarks>
    public static IEnumerable<ConfigurationModel> GetChildren(
        ImmutableArray<ConfigurationModel> configs,
        string parentName)
    {
        return [];
    }

    /// <summary>
    /// Validates the parent/child relationships and returns any errors.
    /// </summary>
    /// <remarks>
    /// Why: Always returns empty — no parent-child relationships exist in [ManagedConfiguration] after FDW-395 Phase 6.
    /// </remarks>
    public static IEnumerable<ParentChildError> Validate(ImmutableArray<ConfigurationModel> configs)
    {
        return [];
    }
}
