using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Data;
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
    // A data store is read with its paths and the containers under them: what a SQL step's script names is judged against them (FDW-5457).
    private const int PathsAndContainers = 2;

    /// <summary>Decides the organization a new graph belongs to (FDW-4573), then judges and saves it.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save; its organization is set from the owner's decision.</param>
    /// <param name="owner">Decides the organization of the new pipeline.</param>
    /// <param name="requestedOrganizationId">The organization the request names; null when it names none.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    /// <param name="mayWriteScripts">Says whether the caller may write scripts (FDW-3676); asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    [Obsolete("Does not check the scripts of the graph's SQL steps (FDW-5457); use the overload that takes the data stores.")]
    public static async Task<IGenericResult<GraphPayload>> Save(
        IPipelineConfigurationProvider provider, GraphPipelineConfiguration graph, PipelineOwner owner, Guid? requestedOrganizationId,
        IDataSetConfigurationProvider dataSets, Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken ct)
    {
        var organization = await owner.OrganizationOfNew(requestedOrganizationId, ct).ConfigureAwait(false);
        if (organization.IsFailure)
        {
            return organization.ToNewResult<GraphPayload>();
        }

        graph.OrgId = organization.Value;
        return await Write(provider, graph, (problemGraph, token) => Problems(problemGraph, dataSets, logger, token), mayWriteScripts, logger, mayCreate: true, ct).ConfigureAwait(false);
    }

    /// <summary>Decides the organization a new graph belongs to (FDW-4573), then judges it, its SQL steps' scripts included (FDW-5457), and saves it.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save; its organization is set from the owner's decision.</param>
    /// <param name="owner">Decides the organization of the new pipeline.</param>
    /// <param name="requestedOrganizationId">The organization the request names; null when it names none.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    /// <param name="dataStores">The data stores, read with their paths and containers for the tables a SQL step's script names.</param>
    /// <param name="mayWriteScripts">Says whether the caller may write scripts (FDW-3676); asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    public static async Task<IGenericResult<GraphPayload>> Save(
        IPipelineConfigurationProvider provider, GraphPipelineConfiguration graph, PipelineOwner owner, Guid? requestedOrganizationId,
        IDataSetConfigurationProvider dataSets, IDataStoreConfigurationProvider dataStores, Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken ct)
    {
        var organization = await owner.OrganizationOfNew(requestedOrganizationId, ct).ConfigureAwait(false);
        if (organization.IsFailure)
        {
            return organization.ToNewResult<GraphPayload>();
        }

        graph.OrgId = organization.Value;
        return await Write(provider, graph, async (problemGraph, token) => (await Judge(problemGraph, dataSets, dataStores, logger, token).ConfigureAwait(false)).Problems, mayWriteScripts, logger, mayCreate: true, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Judges a graph and updates the pipeline it identifies; a pipeline removed since it was read is not brought back.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save, carrying the Id of the pipeline it replaces.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    /// <param name="mayWriteScripts">Says whether the caller may write scripts (FDW-3676); asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    [Obsolete("Does not check the scripts of the graph's SQL steps (FDW-5457); use the overload that takes the data stores.")]
    public static Task<IGenericResult<GraphPayload>> Update(
        IPipelineConfigurationProvider provider, GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets,
        Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken ct)
        => Write(provider, graph, (problemGraph, token) => Problems(problemGraph, dataSets, logger, token), mayWriteScripts, logger, mayCreate: false, ct);

    /// <summary>Judges a graph, its SQL steps' scripts included (FDW-5457), and updates the pipeline it identifies; a pipeline removed since it was read is not brought back.</summary>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="graph">The graph to save, carrying the Id of the pipeline it replaces.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    /// <param name="dataStores">The data stores, read with their paths and containers for the tables a SQL step's script names.</param>
    /// <param name="mayWriteScripts">Says whether the caller may write scripts (FDW-3676); asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">Cancels the save.</param>
    /// <returns>The saved graph as the API states it, or the refusal or failure.</returns>
    public static Task<IGenericResult<GraphPayload>> Update(
        IPipelineConfigurationProvider provider, GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets, IDataStoreConfigurationProvider dataStores,
        Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken ct)
        => Write(provider, graph, async (problemGraph, token) => (await Judge(problemGraph, dataSets, dataStores, logger, token).ConfigureAwait(false)).Problems, mayWriteScripts, logger, mayCreate: false, ct);

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

    /// <summary>
    /// Judges a graph (FDW-5457): the problems it is refused for (its shape, the columns a destination's value sources fill that cannot hold what they
    /// mint, and the scripts of its SQL steps that do not parse or name what their catalogued data store does not hold), and the warnings of what the
    /// script check cannot judge, which do not stop a save.
    /// </summary>
    /// <param name="graph">The graph.</param>
    /// <param name="dataSets">The data sets.</param>
    /// <param name="dataStores">The data stores, read with their paths and containers.</param>
    /// <param name="logger">The logger a refusal or warning is written to.</param>
    /// <param name="ct">Cancels the reads.</param>
    /// <returns>The problems and the warnings, each with its path.</returns>
    public static async Task<GraphFindings> Judge(
        GraphPipelineConfiguration graph, IDataSetConfigurationProvider dataSets, IDataStoreConfigurationProvider dataStores, ILogger logger, CancellationToken ct)
    {
        var scripts = await GraphScriptProblems.Judge(graph, (name, token) => dataStores.Get(name, PathsAndContainers, token), logger, ct).ConfigureAwait(false);
        return new GraphFindings([.. await Problems(graph, dataSets, logger, ct).ConfigureAwait(false), .. scripts.Problems], scripts.Warnings);
    }

    private static async Task<IGenericResult<GraphPayload>> Write(
        IPipelineConfigurationProvider provider, GraphPipelineConfiguration graph,
        Func<GraphPipelineConfiguration, CancellationToken, Task<IReadOnlyList<GraphProblem>>> judge,
        Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, bool mayCreate, CancellationToken ct)
    {
        var access = await GraphScriptAccess.Check(graph, mayWriteScripts, logger, ct).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access.ToNewResult<GraphPayload>();
        }

        var problems = await judge(graph, ct).ConfigureAwait(false);
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
