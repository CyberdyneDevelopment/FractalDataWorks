using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One problem with a graph as the API states it (FDW-1396): where it is and why.</summary>
public class GraphProblemPayload
{
    /// <summary>
    /// Gets or sets the path to the offending part, one name or index per step: <c>["components","map","settings","maps"]</c>,
    /// <c>["components","split","outputs","rest"]</c>, <c>["links","2"]</c>. Empty when the problem concerns the graph as a whole.
    /// </summary>
    public IList<string> Path { get; set; } = [];

    /// <summary>Gets or sets what is wrong, in words written for the person building the graph.</summary>
    public string Reason { get; set; } = string.Empty;
}
