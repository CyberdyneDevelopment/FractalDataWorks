using System;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Creates a workspace.</summary>
/// <remarks>
/// Status, Visibility and JoinPolicy are required rather than defaulted. A project silently
/// created Private and Closed, or silently Open, is a decision the caller did not make — so a
/// missing value is a validation failure, not a substituted one.
/// </remarks>
public class CreateWorkspaceRequest
{
    /// <summary>Gets or sets the unique workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the workspace's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the question this project exists to answer.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the lifecycle status: Draft, Active, Paused or Archived.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project: Private, Discoverable or Open.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets the join policy: Closed, RequestToJoin or AutoApprove.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member role an AutoApprove workspace grants to whoever asks: required when the join
    /// policy is AutoApprove, refused otherwise, and never a role that manages members (Owner, Steward).
    /// </summary>
    public string? AutoApproveRole { get; set; }

    /// <summary>Gets or sets the project-wide seed for generated stand-in values.</summary>
    public string? StandInSeed { get; set; }

    /// <summary>Gets or sets the organization of the caller's tenant to put the workspace in; null for a workspace directly under its tenant.</summary>
    public Guid? OrganizationId { get; set; }
}
