using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A workspace as it appears in a list.</summary>
/// <remarks>
/// Carries both Id and Name. Routes key on name because names make better URLs; a client keys its
/// cache on Id so a rename is not simultaneously a broken link and a cache miss.
/// </remarks>
public class WorkspaceSummaryResponse : ResourceSummary
{
    // No MemberCount/ResourceCount: both were taken from the aggregate's child collections, which
    // the list path does not load, so they reported a confident 0 for a populated workspace rather
    // than disagreeing with the map. No ModifiedAt: ModifyDate is NOT NULL with a default and is
    // stamped at insert, so a never-modified workspace would report a modification, and no screen
    // renders a modified date at all. A field that can only be drawn dishonestly is not sent.

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

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the workspace's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the one-line statement of what this workspace is for.</summary>
    /// <remarks>
    /// Null when nobody has written one. Left null rather than emptied so a client can draw the
    /// absence -- "no purpose recorded" -- instead of an empty line that looks like a rendering bug.
    /// </remarks>
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

    /// <summary>Gets or sets when the project was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
