using System;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.UniverseMembershipRequest</c> — a request to join a universe.
/// </summary>
/// <remarks>
/// Deliberately mirrors <c>msg.AccessRequest</c> column for column so the review path is a shape
/// reviewers already know. It is configuration rather than operational data because approving it
/// changes configuration, following <c>quality.PromotionRequest</c>.
/// </remarks>
[GenerateMapper]
public sealed partial class UniverseMembershipRequestConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the universe being joined.</summary>
    public Guid UniverseId { get; set; }

    /// <summary>Gets or sets who asked.</summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>Gets or sets the role asked for.</summary>
    public string RequestedRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the stated reason for the request.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets the status: Pending, Approved, Declined, Withdrawn or Expired.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who reviewed the request.</summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>Gets or sets when the request was reviewed.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Gets or sets the reviewer's notes.</summary>
    public string? ReviewNotes { get; set; }

    /// <summary>Gets or sets when an unreviewed request lapses.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

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
