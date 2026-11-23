using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The columns of one <c>workspace.WorkspaceResource</c> row that say which workspace holds a resource (FDW-6967).
/// </summary>
/// <remarks>
/// <see cref="WorkspaceResourceConfiguration"/> is the row as a child of its workspace and carries no workspace: the workspace is the
/// parent it is saved under. Asking "who owns this resource" starts from the resource, so the workspace's Id is read from the row itself.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class WorkspaceResourceOwnerRow : IGenericConfiguration
{
    /// <summary>Gets or sets the durable Id of the resource row (the attachment).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name the row was stored with: the resource's label.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of the resource.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the resource's Id.</summary>
    public Guid ResourceId { get; set; }

    /// <summary>Gets or sets how the workspace relates to the resource.</summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable Id of the workspace that holds the resource.</summary>
    public Guid WorkspaceConfigurationId { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
