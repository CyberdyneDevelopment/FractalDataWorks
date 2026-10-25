using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Workspaces;

/// <summary>The contract every Workspace implementation carries.</summary>
public interface IWorkspaceImplementationConfiguration : IImplementationConfiguration
{

    /// <summary>Gets or sets the Workspace DisplayName.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the Workspace Description.</summary>
    string? Description { get; set; }

    /// <summary>
    /// Gets or sets how sensitive this data is: the <c>ClassificationId</c> of a <c>DataClassifications</c>
    /// level. <c>NotYetClassified</c> until someone picks one; never empty. Maps to <c>ClassificationId</c> (FDW-938).
    /// </summary>
    int ClassificationId { get; set; }

    /// <summary>Gets or sets the Workspace Purpose.</summary>
    string? Purpose { get; set; }

    /// <summary>Gets or sets the Workspace Status.</summary>
    string Status { get; set; }

    /// <summary>Gets or sets the Workspace Visibility.</summary>
    string Visibility { get; set; }

    /// <summary>Gets or sets the Workspace JoinPolicy.</summary>
    string JoinPolicy { get; set; }

    /// <summary>
    /// Gets or sets the member role an AutoApprove workspace grants to whoever asks to join it. Required
    /// when <see cref="JoinPolicy"/> is AutoApprove and null otherwise (FDW-1070); never a role that
    /// manages members.
    /// </summary>
    string? AutoApproveRole { get; set; }

    /// <summary>Gets or sets the Workspace OwnerUserId.</summary>
    Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets the Workspace StandInSeed.</summary>
    string? StandInSeed { get; set; }

    /// <summary>Gets or sets the organization of the workspace's tenant this workspace belongs to; null when it sits directly under its tenant (FDW-2338).</summary>
    Guid? OrganizationId { get; set; }

    /// <summary>Gets or sets the Workspace TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the Workspace VisibilityGroupId.</summary>
    Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the Workspace Members.</summary>
    IList<WorkspaceMemberConfiguration> Members { get; set; }

    /// <summary>Gets or sets the Workspace Resources.</summary>
    IList<WorkspaceResourceConfiguration> Resources { get; set; }

    /// <summary>Gets or sets the Workspace Relationships.</summary>
    IList<WorkspaceRelationshipConfiguration> Relationships { get; set; }

    /// <summary>Gets or sets the requests to join the workspace.</summary>
    IList<WorkspaceMembershipRequestConfiguration> MembershipRequests { get; set; }

    /// <summary>Gets or sets the Workspace IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the Workspace IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the Workspace SrcCreateDate.</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the Workspace CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the Workspace CreateBy.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets or sets the Workspace CreateOnBehalfOf.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the Workspace ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the Workspace ModifyBy.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the Workspace ModifyOnBehalfOf.</summary>
    string ModifyOnBehalfOf { get; set; }
}
