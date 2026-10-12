using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// Request to create an access request.
/// </summary>
public sealed class CreateAccessRequest
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the requesting user's identifier.</summary>
    public Guid RequestingUserId { get; set; }

    /// <summary>Gets or sets the resource being requested.</summary>
    public string RequestedResource { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission being requested.</summary>
    public string RequestedPermission { get; set; } = string.Empty;

    /// <summary>Gets or sets the justification for the request.</summary>
    public string? Justification { get; set; }

    /// <summary>Gets or sets a reference identifier for correlation.</summary>
    public string? ReferenceId { get; set; }
}
