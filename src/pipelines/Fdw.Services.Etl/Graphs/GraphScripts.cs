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
        return [.. graph.Components.Where(Holds).Select(component => component.Name)];
    }

    /// <summary>Lists the SQL steps of a graph (FDW-5457): each component that holds script text and names the data store it runs on, with its script.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>The steps, in the order the graph lists them; a component that names no data store is left out, as the graph check refuses it.</returns>
    public static IReadOnlyList<GraphScript> Held(GraphPipelineConfiguration graph)
    {
        return [.. graph.Components.SelectMany(StepsOf)];
    }

    private static IEnumerable<GraphScript> StepsOf(PipelineComponentConfiguration component) =>
        component.DataStoreName is { } store && !string.IsNullOrWhiteSpace(store)
            ? TextsOf(component).Select(text => new GraphScript(component.Name, store, text))
            : [];

    private static IEnumerable<string> TextsOf(PipelineComponentConfiguration component) =>
        ComponentTypes.ByName(component.ComponentType).Settings
            .Where(setting => string.Equals(setting.Kind, SettingKind, StringComparison.Ordinal))
            .Select(setting => ComponentSettingBindings.Find(setting.Name)?.Get(component))
            .OfType<string>()
            .Where(text => !string.IsNullOrWhiteSpace(text));

    private static bool Holds(PipelineComponentConfiguration component) =>
        ComponentTypes.ByName(component.ComponentType).Settings
            .Where(setting => string.Equals(setting.Kind, SettingKind, StringComparison.Ordinal))
            .Any(setting => ComponentSettingBindings.Find(setting.Name)?.Get(component) is string text && !string.IsNullOrWhiteSpace(text));
}
