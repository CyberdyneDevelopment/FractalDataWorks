using System;

namespace Fdw.Services.Messaging.Clients.Models;

/// <summary>
/// Client-side payload for access requests.
/// Mirrors the API contract from Fdw.Services.Messaging.
/// </summary>
public sealed class AccessRequestPayload
{
    /// <summary>Gets or sets the unique access-request identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the message that carries this access request.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the resource to which access is being requested.</summary>
    public string RequestedResource { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission being requested on the resource.</summary>
    public string RequestedPermission { get; set; } = string.Empty;

    /// <summary>Gets or sets the requester's justification, if provided.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets the current review status of the request.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the user who reviewed the request, if reviewed.</summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>Gets or sets when the request was reviewed, if applicable.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Gets or sets the reviewer's notes, if any.</summary>
    public string? ReviewNotes { get; set; }

    /// <summary>Gets or sets when the granted access expires, if applicable.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets when the request was created.</summary>
    public DateTime CreatedAt { get; set; }
}
