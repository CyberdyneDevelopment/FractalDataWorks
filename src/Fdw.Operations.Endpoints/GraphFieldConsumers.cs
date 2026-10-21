using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Finds which graph pipelines use the fields of a data set, and what each field becomes there (FDW-2157), from the graph's own field flow: the
/// describe call computes the fields at every node and, since FDW-2156, which field of which data set each one came from.
/// </summary>
/// <remarks>
/// Only exact relations are reported. A field is a consumer when it arrives at a destination component's input with an origin that is the field
/// asked about, so a renamed field is followed and a field the flow made is not. A pipeline that reads the data set and has no such field at any
/// destination is not guessed at: it is named as not followed, so an empty list never hides a pipeline the flow could not place.
/// </remarks>
internal static class GraphFieldConsumers
{
    /// <summary>Adds the graph pipelines that use the fields of a data set to its consumers.</summary>
    /// <param name="dataSetName">The data set.</param>
    /// <param name="fields">The names of its fields.</param>
    /// <param name="consumers">The consumers by field name; pipelines are added to it.</param>
    /// <param name="graphs">Every graph pipeline, read whole.</param>
    /// <param name="dataSetFields">Reads the fields of a data set, as the pipeline builder describes a graph.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The names of the pipelines that read the data set and could not be followed.</returns>
    public static async Task<IReadOnlyList<string>> Add(
        string dataSetName,
        HashSet<string> fields,
        Dictionary<string, List<FieldConsumerResponse>> consumers,
        IReadOnlyList<GraphPipelineConfiguration> graphs,
        Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> dataSetFields,
        ILogger logger,
        CancellationToken ct)
    {
        var notFollowed = new List<string>();
        foreach (var graph in graphs.Where(graph => PipelineLineageProjection.From(graph, logger).DataSetsRead.Contains(dataSetName, StringComparer.OrdinalIgnoreCase)))
        {
            var nodes = await GraphDescriber.Describe(graph, new DescribeContext(dataSetFields), logger, ct).ConfigureAwait(false);
            if (nodes.IsFailure)
            {
                FieldConsumerScanLog.PipelineNotFollowed(logger, graph.Name, dataSetName, nodes.CurrentMessage);
                notFollowed.Add(graph.Name);
                continue;
            }

            var placed = Place(dataSetName, fields, consumers, graph, nodes.Value!);
            if (placed == 0)
            {
                FieldConsumerScanLog.PipelineNotFollowed(logger, graph.Name, dataSetName, "no field of the data set reaches a destination");
                notFollowed.Add(graph.Name);
            }
        }

        return notFollowed;
    }

    // The fields that arrive at the input of each destination component, as the destination sees them after its own mapping (FDW-2183: the node's own fields are the
    // ones before it).
    private static int Place(
        string dataSetName, HashSet<string> fields, Dictionary<string, List<FieldConsumerResponse>> consumers,
        GraphPipelineConfiguration graph, IReadOnlyList<GraphNodeFields> nodes)
    {
        var destinations = graph.Components
            .Where(component => string.Equals(ComponentTypes.ByName(component.ComponentType).Category, "Destination", StringComparison.Ordinal))
            .Select(component => component.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var placed = 0;
        foreach (var node in nodes.Where(node => node.IsInput && destinations.Contains(node.Component) && node.Seen is not null))
        {
            foreach (var field in node.Seen!.Where(field => field.Origin is { } origin
                && string.Equals(origin.DataSet, dataSetName, StringComparison.OrdinalIgnoreCase) && fields.Contains(origin.Field)))
            {
                consumers[field.Origin!.Field].Add(new FieldConsumerResponse
                {
                    ConsumerKind = "Pipeline",
                    ConsumerName = graph.Name,
                    ConsumerComponent = node.Component,
                    ConsumerField = field.Name,
                });
                placed++;
            }
        }

        return placed;
    }
}
