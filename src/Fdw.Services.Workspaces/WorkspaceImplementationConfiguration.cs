using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Maps to <c>workspace.Workspace</c> — a collaborative data project.
/// </summary>
/// <remarks>
/// <para>
/// A Workspace is the root of the collaboration hierarchy: members join it, resources belong to
/// it, and the data sets it owns need not be backed by a source yet.
/// </para>
/// <para>
/// Why a Workspace names no implementation: it is not dispatched to a factory. It is
/// a configuration record, not a service with pluggable implementations, so it carries no option
/// discriminator — the same as <c>OrchestrationNodeImplementationConfiguration</c> and its Stage/Step siblings.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Workspace")]
public partial class WorkspaceImplementationConfiguration
    : IWorkspaceImplementationConfiguration
{
    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <summary>Gets or sets the durable logical identity, minted by the caller before insert.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the unique workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets how sensitive this data is: the <c>ClassificationId</c> of a <c>DataClassifications</c>
    /// level. <c>NotYetClassified</c> until someone picks one; never empty. Maps to <c>ClassificationId</c> (FDW-938).
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the question this project exists to answer.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the lifecycle status: Draft, Active, Paused or Archived.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project: Private, Discoverable or Open.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets what happens when someone asks to join: Closed, RequestToJoin or AutoApprove.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member role an AutoApprove workspace grants to whoever asks: required when
    /// <see cref="JoinPolicy"/> is AutoApprove and null otherwise (FDW-1070). Never Owner or Steward:
    /// a role that manages members is never handed to whoever asks.
    /// </summary>
    public string? AutoApproveRole { get; set; }

    /// <summary>Gets or sets the owning user.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>
    /// Gets or sets the project-wide seed for generated stand-in values. A fixed seed keeps
    /// stand-ins stable, so a saved chart does not change shape on refresh. A field may override it.
    /// </summary>
    public string? StandInSeed { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the members of this workspace.</summary>
    public IList<WorkspaceMemberConfiguration> Members { get; set; } = [];

    /// <summary>Gets or sets the resources attached to this workspace.</summary>
    public IList<WorkspaceResourceConfiguration> Resources { get; set; } = [];

    /// <summary>Gets or sets the declared relationships between this workspace's data sets.</summary>
    public IList<WorkspaceRelationshipConfiguration> Relationships { get; set; } = [];

    /// <summary>Gets or sets the requests to join this workspace.</summary>
    public IList<WorkspaceMembershipRequestConfiguration> MembershipRequests { get; set; } = [];

    /// <summary>Gets or sets whether this is the current active version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
