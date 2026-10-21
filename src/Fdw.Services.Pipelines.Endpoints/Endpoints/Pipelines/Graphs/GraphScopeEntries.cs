using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What every step of a graph can read by name (FDW-2006): the graph's parameters and variables, and the facts about the run the engine declares in every graph
/// (<see cref="RunFacts"/>). The describe answer lists them so a person is offered what exists and the console keeps no names of its own.
/// </summary>
public static class GraphScopeEntries
{
    /// <summary>The data type of every run fact: a date with a time and an offset.</summary>
    public const string RunFactDataType = "DateTimeOffset";

    /// <summary>Lists what a graph's steps can read.</summary>
    /// <param name="graph">The graph or draft.</param>
    /// <returns>
    /// The parameters, then the variables, then the run facts. A loop's own counter or element is the loop's, so it is not listed. A run fact is read-only and is no parameter.
    /// </returns>
    public static IReadOnlyList<GraphScopeEntryPayload> Of(GraphPipelineConfiguration graph)
    {
        System.ArgumentNullException.ThrowIfNull(graph);
        return
        [
            .. graph.Parameters.Select(parameter => new GraphScopeEntryPayload
            {
                Name = parameter.Name, DataType = parameter.ParameterType, IsParameter = true, IsReadOnly = parameter.IsReadOnly, IsRequired = parameter.IsRequired,
            }),
            .. graph.Variables.Select(variable => new GraphScopeEntryPayload { Name = variable.Name, DataType = variable.DataType }),
            .. RunFacts.Names.Select(fact => new GraphScopeEntryPayload { Name = fact, DataType = RunFactDataType, IsReadOnly = true }),
        ];
    }
}
