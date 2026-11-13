using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One problem the check before save found in a Graph data set draft (FDW-5290).</summary>
/// <remarks>The members of a graph pipeline's problem (<c>path</c>, and <c>reason</c> here named <c>message</c>) and the result code of the problem.</remarks>
public sealed class GraphDataSetProblemPayload
{
    /// <summary>Gets or sets the result code, for example <c>DATA-21400</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets what is wrong.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets where: <c>["fieldsOut", "Amount"]</c>, <c>["components", "Join1"]</c> or <c>["links", "3"]</c>; empty when the problem concerns the whole draft.</summary>
    public IList<string> Path { get; set; } = [];
}
