using System;
using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A workspace with its members, resources and relationships.</summary>
public class WorkspaceDetailResponse
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant that owns the workspace. A name is unique within a tenant only, so a caller who sees
    /// more than one tenant tells same-named workspaces apart by it (FDW-1101).
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the organization that names this workspace as its default, or <see langword="null"/> when none does.
    /// </summary>
    public WorkspaceOrganizationReference? DefaultForOrganization { get; set; }

    /// <summary>
    /// Gets or sets the organization of the workspace's tenant this workspace belongs to, or <see langword="null"/> when it sits directly under its tenant.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Gets or sets the unique workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the workspace's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the question this project exists to answer.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets what happens when someone asks to join.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>Gets or sets the member role the workspace grants when its join policy auto-approves, or null.</summary>
    public string? AutoApproveRole { get; set; }

    /// <summary>Gets or sets the owning user.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets the project-wide seed for generated stand-in values.</summary>
    public string? StandInSeed { get; set; }

    /// <summary>Gets or sets the members.</summary>
    public IList<WorkspaceMemberDto> Members { get; set; } = [];

    /// <summary>Gets or sets the attached resources.</summary>
    public IList<WorkspaceResourceDto> Resources { get; set; } = [];

    /// <summary>Gets or sets the declared relationships.</summary>
    public IList<WorkspaceRelationshipDto> Relationships { get; set; } = [];

    /// <summary>Gets or sets when the project was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the project was last modified.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
