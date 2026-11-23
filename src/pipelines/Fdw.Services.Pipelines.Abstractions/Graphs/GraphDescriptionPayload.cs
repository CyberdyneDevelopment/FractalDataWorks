using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to describing a graph draft (FDW-1397): the fields at every node and the problems the draft has.</summary>
public class GraphDescriptionPayload
{
    /// <summary>Gets or sets the fields at every input and output node, components in link order.</summary>
    public IList<GraphNodeDescriptionPayload> Nodes { get; set; } = [];

    /// <summary>Gets or sets the problems the draft has, as a check of it would list them.</summary>
    public IList<GraphProblemPayload> Problems { get; set; } = [];

    /// <summary>Gets or sets what the answer could not judge and does not count against the draft (FDW-6718): a draft that states no implementation says here that the requirements of the type were not judged.</summary>
    public IList<GraphProblemPayload> Warnings { get; set; } = [];

    /// <summary>Gets or sets the parameters and variables the graph puts in scope for its steps, with their types (FDW-1549).</summary>
    public IList<GraphScopeEntryPayload> Scope { get; set; } = [];
}
