using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// Data transfer object for access requests.
/// </summary>
[GenerateMapper]
public sealed class AccessRequestPayload
{
    /// <summary>Gets or sets the access request identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the associated message identifier.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the resource being requested.</summary>
    public string RequestedResource { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission being requested.</summary>
    public string RequestedPermission { get; set; } = string.Empty;

    /// <summary>Gets or sets the justification for the request.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets the request status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the user who reviewed this request.</summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>Gets or sets when the request was reviewed.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Gets or sets the reviewer's notes.</summary>
    public string? ReviewNotes { get; set; }

    /// <summary>Gets or sets when the granted access expires.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets when the request was created.</summary>
    public DateTime CreatedAt { get; set; }
}
