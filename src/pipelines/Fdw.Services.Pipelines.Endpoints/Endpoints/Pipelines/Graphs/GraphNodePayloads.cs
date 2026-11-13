using System.Linq;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Maps the fields the engine describes at a node of a graph to the answer of a describe route (FDW-5504), so the describe of a graph pipeline and the describe
/// of a Graph data set draft answer a node the same way.
/// </summary>
public static class GraphNodePayloads
{
    /// <summary>Maps one node.</summary>
    /// <param name="node">The node and the fields a row has there, as the engine described them; no fields when the engine could not say.</param>
    /// <returns>The node as the describe routes answer it.</returns>
    public static GraphNodeDescriptionPayload Of(GraphNodeFields node) => new()
    {
        Component = node.Component,
        Node = node.Node,
        IsInput = node.IsInput,
        Fields = node.Fields is null
            ? null
            : [.. node.Fields.Select(field => new GraphFieldPayload
            {
                Name = field.Name,
                Type = field.Type,
                Origin = field.Origin is { } origin ? new GraphFieldOriginPayload { DataSet = origin.DataSet, Field = origin.Field } : null,
            })],
    };
}
