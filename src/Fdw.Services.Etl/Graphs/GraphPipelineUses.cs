using System;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Reads what a graph pipeline uses from what it stores (FDW-1683): the data sets and containers its sources read and its destinations
/// write, the pipelines its actions and stages start, and the data stores its SQL actions run against.
/// </summary>
/// <remarks>
/// Nothing is typed in twice: the uses are the settings the pipeline already holds, so a change to the pipeline changes what it uses. The
/// pipeline must be read whole (its components); a pipeline read as a record alone has no steps and uses nothing.
/// Whether a component reads or writes is what the component catalogue says it is: a component with inputs and no outputs is a
/// destination and writes; every other reads. A component whose type the catalogue does not know says nothing about its reads or writes,
/// because the graph check refuses such a graph before it can run.
/// </remarks>
public static class GraphPipelineUses
{
    private const string DataSet = PipelineUseKinds.DataSet;
    private const string DataStore = PipelineUseKinds.DataStore;
    private const string Container = PipelineUseKinds.Container;
    private const string Pipeline = PipelineUseKinds.Pipeline;
    private const string Transformation = PipelineUseKinds.Transformation;

    // The FromKind a Compute table carries when it is bound to a data set (the other kinds are Stage and Output).
    private const string ComputeTableFromDataSet = "DataSet";

    /// <summary>Lists what a graph pipeline uses, each use once, without the transformations its tools name.</summary>
    /// <param name="graph">The pipeline, read whole.</param>
    /// <returns>The uses, in the order the pipeline's components are listed.</returns>
    public static IReadOnlyList<PipelineUse> Of(GraphPipelineConfiguration graph) => Of(graph, new Dictionary<Guid, string>());

    /// <summary>Lists what a graph pipeline uses, each use once, including the transformations its tools name.</summary>
    /// <param name="graph">The pipeline, read whole.</param>
    /// <param name="transformationNames">The name of each transformation by its id; a tool names its transformation by id, and one with no name here is not listed.</param>
    /// <returns>The uses, in the order the pipeline's components are listed.</returns>
    public static IReadOnlyList<PipelineUse> Of(GraphPipelineConfiguration graph, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(transformationNames);
        var uses = new List<PipelineUse>();
        foreach (var component in graph.Components)
        {
            if (ComponentTypes.ByName(component.ComponentType).IsAction)
            {
                AddAction(uses, component);
            }
            else
            {
                AddComponent(uses, component, transformationNames);
            }
        }

        return uses;
    }

    /// <summary>Lists what a graph pipeline uses for a run given these values, each use once.</summary>
    /// <remarks>
    /// Why the components are rendered first: a source or destination may name its data set or container by a parameter (<c>{{Which}}</c>, or a
    /// setting bound to a parameter), and what the run reads is the name it is given. Each component is rendered by the same
    /// <see cref="ComponentParameters"/> the run builds with, so the roles are judged for what the run will reach and not for the
    /// placeholder text. A value the run was not given fails here, naming the component, the setting and the parameter, as it would when the run built.
    /// </remarks>
    /// <param name="graph">The pipeline, read whole.</param>
    /// <param name="parameters">The run's typed values by parameter name; the variables the pipeline declares and no component changes render with their starting values (FDW-1942).</param>
    /// <param name="logger">The logger the failures are written to.</param>
    /// <returns>The uses, in the order the pipeline's components are listed, or the failure that names the parameter the run was not given.</returns>
    public static IGenericResult<IReadOnlyList<PipelineUse>> Of(GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(logger);
        var scope = GraphScope.RenderValues(graph, parameters, logger);
        if (scope.IsFailure || scope.Value is null)
        {
            return scope.ToNewResult<IReadOnlyList<PipelineUse>>();
        }

        var uses = new List<PipelineUse>();
        var noNames = new Dictionary<Guid, string>();
        foreach (var component in graph.Components)
        {
            if (ComponentTypes.ByName(component.ComponentType).IsAction)
            {
                AddAction(uses, component);
                continue;
            }

            var rendered = ComponentParameters.Render(component, scope.Value, logger);
            if (rendered.IsFailure || rendered.Value is null)
            {
                return rendered.ToNewResult<IReadOnlyList<PipelineUse>>();
            }

            AddComponent(uses, rendered.Value, noNames);
        }

        return GenericResult<IReadOnlyList<PipelineUse>>.Success(uses);
    }

    // An action or loop uses what it names: the pipeline an Execute pipeline starts, the data store an Execute SQL runs against.
    private static void AddAction(List<PipelineUse> uses, PipelineComponentConfiguration action)
    {
        if (!string.IsNullOrWhiteSpace(action.TargetPipeline))
        {
            Add(uses, new PipelineUse(PipelineUseRelations.Calls, Pipeline, action.TargetPipeline, []));
        }

        if (!string.IsNullOrWhiteSpace(action.DataStoreName))
        {
            Add(uses, new PipelineUse(PipelineUseRelations.Uses, DataStore, action.DataStoreName, []));
        }
    }

    private static void AddComponent(List<PipelineUse> uses, PipelineComponentConfiguration component, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        var kind = ComponentTypes.ByName(component.ComponentType);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return;
        }

        var relation = kind.Outputs.Count == 0 ? PipelineUseRelations.Writes : PipelineUseRelations.Reads;
        if (!string.IsNullOrWhiteSpace(component.DataSetName))
        {
            Add(uses, Addressed(new PipelineUse(relation, DataSet, component.DataSetName, []), component, relation));
        }

        if (!string.IsNullOrWhiteSpace(component.ContainerName) && !string.IsNullOrWhiteSpace(component.DataStoreName))
        {
            Add(uses, Addressed(new PipelineUse(relation, Container, component.ContainerName, ParentsOf(component)), component, relation));
        }
        else if (!string.IsNullOrWhiteSpace(component.DataStoreName))
        {
            // Why a read: a component that names a store and no container (a SQL source) selects from it.
            Add(uses, new PipelineUse(PipelineUseRelations.Reads, DataStore, component.DataStoreName, []));
        }

        foreach (var stage in component.Stages)
        {
            AddStage(uses, stage, transformationNames);
        }
    }

    private static void AddStage(List<PipelineUse> uses, PipelineTransformConfiguration stage, IReadOnlyDictionary<Guid, string> transformationNames)
    {
        if (stage.ComputeTransformationId is { } id && transformationNames.TryGetValue(id, out var transformation))
        {
            Add(uses, new PipelineUse(PipelineUseRelations.Uses, Transformation, transformation, []));
        }

        foreach (var lookup in stage.Lookups)
        {
            AddLookup(uses, lookup);
        }

        foreach (var table in stage.ComputeTables.Where(table => string.Equals(table.FromKind, ComputeTableFromDataSet, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(table.FromName)))
        {
            Add(uses, new PipelineUse(PipelineUseRelations.Reads, DataSet, table.FromName!, []));
        }
    }

    // Why a read: a lookup brings columns in from the data set or container it names, and never writes to it.
    private static void AddLookup(List<PipelineUse> uses, PipelineTransformLookupConfiguration lookup)
    {
        if (!string.IsNullOrWhiteSpace(lookup.LookupDataSet))
        {
            Add(uses, new PipelineUse(PipelineUseRelations.Reads, DataSet, lookup.LookupDataSet, []));
        }

        if (!string.IsNullOrWhiteSpace(lookup.LookupContainerName) && !string.IsNullOrWhiteSpace(lookup.LookupDataStoreName))
        {
            Add(uses, new PipelineUse(
                PipelineUseRelations.Reads,
                Container,
                lookup.LookupContainerName,
                string.IsNullOrWhiteSpace(lookup.LookupDataStorePath) ? [lookup.LookupDataStoreName] : [lookup.LookupDataStoreName, lookup.LookupDataStorePath]));
        }
    }

    // The store and then the path when the component names one; the container sits under both.
    private static string[] ParentsOf(PipelineComponentConfiguration component) =>
        string.IsNullOrWhiteSpace(component.PathName)
            ? [component.DataStoreName!]
            : [component.DataStoreName!, component.PathName];

    private static void Add(List<PipelineUse> uses, PipelineUse use)
    {
        if (!uses.Any(existing => existing.Matches(use)))
        {
            uses.Add(use);
        }
    }

    // A destination that writes one file writes that file, not the container as declared (FDW-3193): the use says which, so two runs that write different files
    // of one container do not wait for each other and two that write the same file do.
    private static PipelineUse Addressed(PipelineUse use, PipelineComponentConfiguration component, string relation) =>
        string.Equals(relation, PipelineUseRelations.Writes, StringComparison.Ordinal)
        && string.Equals(component.TargetKind, TargetKinds.File.Name, StringComparison.OrdinalIgnoreCase)
            ? use with { PhysicalPath = component.PhysicalPath, PhysicalName = component.PhysicalName }
            : use;
}
