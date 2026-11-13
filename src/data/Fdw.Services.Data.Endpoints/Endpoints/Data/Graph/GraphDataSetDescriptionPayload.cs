using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The answer of <c>POST /datasets/graph/describe</c> (FDW-5504): the fields a row has at every node of a Graph data set draft, and every problem of the draft.</summary>
/// <remarks>
/// The nodes and the scope are the shapes <c>POST /pipelines/graph/describe</c> answers, so a console reads them the same way. The problems are those of
/// <c>POST /datasets/graph/validate</c>, each with its code and path. A draft with a problem still describes what the engine can; the problems say what it could not.
/// </remarks>
public sealed class GraphDataSetDescriptionPayload
{
    /// <summary>Gets or sets a value indicating whether the draft has no problem.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the fields a row has at every input and output node, one entry for each node.</summary>
    public IList<GraphNodeDescriptionPayload> Nodes { get; set; } = [];

    /// <summary>Gets or sets what a calculation in the graph may name, as the pipeline describe answers it.</summary>
    public IList<GraphScopeEntryPayload> Scope { get; set; } = [];

    /// <summary>Gets or sets every problem of the draft, each with its code and path.</summary>
    public IList<GraphDataSetProblemPayload> Problems { get; set; } = [];
}
