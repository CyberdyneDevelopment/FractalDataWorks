using System;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Judges how deep a graph nests the components that hold components, against the setting <see cref="SettingDefinitions.MaxContainerDepth"/> (FDW-6134). A graph that nests none is
/// not judged and needs no setting; a graph that does is refused when the setting is not set to a positive number, and when it nests deeper than the setting says. No depth is assumed.
/// </summary>
public static class GraphContainerDepthJudgment
{
    /// <summary>Judges the graph.</summary>
    /// <param name="graph">The graph a save states.</param>
    /// <param name="settings">Where the setting is read.</param>
    /// <param name="tenantId">The caller's tenant, which the setting resolves for before the server's value; none when the caller acts in none.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the refusal the save answers.</returns>
    public static IGenericResult Judge(GraphPipelineConfiguration graph, IEffectiveSettingsProvider settings, Guid? tenantId, ILogger logger)
    {
        var (component, depth) = GraphContainerDepth.Deepest(graph);
        if (component is null)
        {
            return GenericResult.Success();
        }

        var maximum = settings.GetEffectiveValue<int>(SettingDefinitions.MaxContainerDepth, tenantId);
        if (maximum <= 0)
        {
            return GenericResult.Failure(GraphImplementationLog.ContainerDepthNotSet(logger, graph.Name, SettingDefinitions.MaxContainerDepth));
        }

        return depth > maximum
            ? GenericResult.Failure(GraphImplementationLog.ContainerTooDeep(logger, graph.Name, component, depth, maximum))
            : GenericResult.Success();
    }
}
