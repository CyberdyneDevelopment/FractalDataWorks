using System.Text.Json.Serialization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One step data takes from one node to another.</summary>
public sealed class LineageEdgeDto
{
    /// <summary>Gets or sets the node id data leaves.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Gets or sets the node id data arrives at.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Gets or sets the relation: pipeline, derived_from or stored_in.</summary>
    public string Relation { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline the edge passes through, or null when it passes through none.</summary>
    public LineageViaDto? Via { get; set; }

    /// <summary>
    /// Gets or sets whether the edge is intended: a Graph data set says it will read from the origin and nothing reads it yet (FDW-5296). True for such an edge and
    /// left out (null) for every other, so no answer for any other data set changes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Intended { get; set; }
}
