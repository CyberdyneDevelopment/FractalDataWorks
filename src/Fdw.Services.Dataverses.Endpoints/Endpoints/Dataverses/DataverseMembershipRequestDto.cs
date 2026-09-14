using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>A request to join a dataverse.</summary>
public class DataverseMembershipRequestDto
{
    /// <summary>Gets or sets the request's logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets who asked.</summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>Gets or sets what the membership would be for: User or Role.</summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the user or role the membership would be for.</summary>
    public Guid SubjectId { get; set; }

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
}
