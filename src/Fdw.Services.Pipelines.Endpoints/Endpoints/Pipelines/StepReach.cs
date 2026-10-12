using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What a pipeline's stages and load steps touch beyond its own source and destination (FDW-1161): the data sets its Union
/// stages append, its Lookup stages read and the tables of its Compute stages are bound to, the connections its Lookup stages name, the data sets its load and reject steps
/// write or read, and the containers its Execute steps write.
/// </summary>
/// <remarks>
/// Why this is read from the saved pipeline and nowhere else: a run is judged for reach when it is triggered or scheduled,
/// against the pipeline as it is then stored, so one reading of the stored detail answers for every way of starting a run.
/// Every name appears once, however many stages and steps name it.
/// </remarks>
/// <param name="DataSets">The data sets the stages and steps read or write.</param>
/// <param name="Connections">The connections the stages name.</param>
/// <param name="Containers">The data store containers the steps write.</param>
public sealed record StepReach(
    IReadOnlyList<ReachedDataSet> DataSets,
    IReadOnlyList<ReachedConnection> Connections,
    IReadOnlyList<ReachedContainer> Containers)
{
    /// <summary>Reads what the stages and load steps of a saved pipeline touch.</summary>
    /// <param name="pipeline">The saved pipeline as its detail reads it.</param>
    /// <returns>What the pipeline's stages and steps read and write.</returns>
    public static StepReach Of(PipelineDetailResponse pipeline)
    {
        var dataSets = new List<ReachedDataSet>();
        var connections = new List<ReachedConnection>();
        var containers = new List<ReachedContainer>();
        Stages(pipeline.Transforms, dataSets, connections);
        Steps(pipeline.LoadSteps, dataSets, containers);
        Steps(pipeline.RejectSteps, dataSets, containers);
        return new StepReach(
            [.. dataSets.Distinct()],
            [.. connections.Distinct()],
            [.. containers.Distinct()]);
    }

    private static void Stages(IEnumerable<PipelineTransformDto>? stages, List<ReachedDataSet> dataSets, List<ReachedConnection> connections)
    {
        if (stages is null)
        {
            return;
        }

        foreach (var stage in stages)
        {
            if (!string.IsNullOrWhiteSpace(stage.UnionSource))
            {
                dataSets.Add(new ReachedDataSet(stage.UnionSource, false));
            }

            if (stage.Lookup is { } lookup)
            {
                Add(dataSets, lookup.LookupDataSet, false);
                Add(connections, lookup.LookupConnectionName, false);
            }

            foreach (var table in stage.Compute?.Tables.Where(table => string.Equals(table.From, "DataSet", System.StringComparison.Ordinal)) ?? [])
            {
                Add(dataSets, table.Name, false);
            }

            if (stage.Paths is null)
            {
                continue;
            }

            foreach (var path in stage.Paths)
            {
                Stages(path.Stages, dataSets, connections);
            }
        }
    }

    private static void Steps(IEnumerable<PipelineLoadStep>? steps, List<ReachedDataSet> dataSets, List<ReachedContainer> containers)
    {
        if (steps is null)
        {
            return;
        }

        foreach (var step in steps)
        {
            Add(dataSets, step.DestinationDataSet, true);
            Add(dataSets, step.Options?.SourceDataSet, false);
            if (step.Options?.DestinationContainer is { } container && !string.IsNullOrWhiteSpace(container.DataStoreName))
            {
                containers.Add(new ReachedContainer(container.DataStoreName, container.PathName, container.ContainerName, true));
            }
        }
    }

    private static void Add(List<ReachedDataSet> dataSets, string? name, bool writes)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            dataSets.Add(new ReachedDataSet(name, writes));
        }
    }

    private static void Add(List<ReachedConnection> connections, string? name, bool writes)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            connections.Add(new ReachedConnection(name, writes));
        }
    }
}
