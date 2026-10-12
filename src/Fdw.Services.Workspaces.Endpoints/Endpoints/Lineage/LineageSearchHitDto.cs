using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One match of a lineage search, for picking a focus.</summary>
public sealed class LineageSearchHitDto
{
    /// <summary>Gets or sets the node id, kind-prefixed as on the map.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: DataSet, DataStore or Pipeline.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspace the request was scoped to, or null when it was not scoped to one.</summary>
    public string? Workspace { get; set; }

    /// <summary>Gets or sets the chain that leads to the hit, root first and the hit last; nothing unless the paged form was asked for.</summary>
    /// <remarks>Only the workspace the search was scoped to can root a path: a search over everything gives the hit alone.</remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LineagePathStepDto>? Path { get; set; }
}
