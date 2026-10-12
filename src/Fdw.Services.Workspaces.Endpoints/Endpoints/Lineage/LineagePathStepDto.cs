using System.Text.Json.Serialization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One step of the chain that leads to a search hit: where a client opens to focus it, root first.</summary>
public sealed class LineagePathStepDto
{
    /// <summary>Gets or sets the step's id, kind-prefixed as on the map; nothing when the step has no node id.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>Gets or sets the step's kind: Workspace, DataSet, DataStore or Pipeline.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the step's name.</summary>
    public string Name { get; set; } = string.Empty;
}
