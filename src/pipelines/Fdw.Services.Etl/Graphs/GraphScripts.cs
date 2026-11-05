using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Finds the script text a stored graph holds (FDW-3676). A component holds script text when its kind has a setting of kind <c>SqlText</c> and the
/// component states that setting.
/// </summary>
/// <remarks>
/// It reads the kinds' own settings, so a kind that takes script text is found with no change here.
/// </remarks>
public static class GraphScripts
{
    /// <summary>The setting kind that holds script text.</summary>
    public const string SettingKind = "SqlText";

    /// <summary>Lists the components of a graph that hold script text.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>The names of the components, in the order the graph lists them; empty when the graph holds no script text.</returns>
    public static IReadOnlyList<string> ComponentsHolding(GraphPipelineConfiguration graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return [.. graph.Components.Where(Holds).Select(component => component.Name)];
    }

    private static bool Holds(PipelineComponentConfiguration component) =>
        ComponentTypes.ByName(component.ComponentType).Settings
            .Where(setting => string.Equals(setting.Kind, SettingKind, StringComparison.Ordinal))
            .Any(setting => ComponentSettingBindings.Find(setting.Name)?.Get(component) is string text && !string.IsNullOrWhiteSpace(text));
}
