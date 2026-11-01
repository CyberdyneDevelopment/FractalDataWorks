using System;
using System.Text.Json.Serialization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What a map response covers, echoed first so a client knows what it is holding before the nodes arrive.</summary>
public sealed class MapScopeDto
{
    /// <summary>Gets or sets the kind of scope: everything, organization, workspace or children.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspace's name, for a workspace scope.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Workspace { get; set; }

    /// <summary>Gets or sets the organization's id, for an organization scope.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? OrganizationId { get; set; }

    /// <summary>Gets or sets whether the caller may change what the workspace holds and how its data sets are joined, for a workspace scope.</summary>
    /// <remarks>The workspace's own write decision: it covers attaching and detaching resources and declaring joins, and it says nothing about editing the objects the workspace holds (see <see cref="MapNodeDto.CanEdit"/>).</remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CanEdit { get; set; }

    /// <summary>Gets or sets the id of the node that was exploded, for a children response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentNodeId { get; set; }
}
