using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The save every graph write shares (FDW-1396): judge the graph, write it through the pipeline domain's provider, and answer with
/// what a later read of the same pipeline returns.
/// </summary>
public static class GraphSave
{
    /// <summary>Judges and saves a graph.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    /// <param name="mayWriteScripts">Says whether the caller may write scripts (FDW-3676); asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    public static Task<IGenericResult<GraphPayload>> Save(
        PipelineServiceConfigurationProvider provider, GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets,
        Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken ct)
        => Write(provider, graph, dataSets, mayWriteScripts, logger, mayCreate: true, ct);

    /// <summary>Judges a graph and updates the pipeline it identifies; a pipeline removed since it was read is not brought back.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save, carrying the Id of the pipeline it replaces.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    /// <param name="mayWriteScripts">Says whether the caller may write scripts (FDW-3676); asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    public static Task<IGenericResult<GraphPayload>> Update(
        PipelineServiceConfigurationProvider provider, GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets,
        Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken ct)
        => Write(provider, graph, dataSets, mayWriteScripts, logger, mayCreate: false, ct);

    /// <summary>Lists what a graph is refused for: its shape, and the columns a destination's value sources fill that cannot hold what they mint.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="dataSets">The data sets.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the reads.</param>
    /// <returns>The problems, each with its path.</returns>
    public static async Task<IReadOnlyList<GraphProblem>> Problems(
        GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets, ILogger logger, CancellationToken ct) =>
    [
        .. GraphShapeValidator.Problems(graph, logger),
        .. await DestinationGuidColumns.Problems(graph, (name, token) => GraphDataSetFields.Read(dataSets, name, token), logger, ct).ConfigureAwait(false),
    ];

    private static async Task<IGenericResult<GraphPayload>> Write(
        PipelineServiceConfigurationProvider provider, GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets,
        Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, bool mayCreate, CancellationToken ct)
    {
        var access = await GraphScriptAccess.Check(graph, mayWriteScripts, logger, ct).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access.ToNewResult<GraphPayload>();
        }

        var problems = await Problems(graph, dataSets, logger, ct).ConfigureAwait(false);
        if (problems.Count > 0)
        {
            return GraphRefusal.Of<GraphPayload>(string.Join("; ", problems.Select(problem => problem.Reason)));
        }

        var saved = mayCreate
            ? await provider.Save(graph, "Pipeline", graph.Implementation, graph.Name, ct).ConfigureAwait(false)
            : await provider.Update(graph, "Pipeline", graph.Implementation, graph.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<GraphPayload>();
        }

        return await GraphRead.Read(provider, graph.Name, ct).ConfigureAwait(false);
    }
}
