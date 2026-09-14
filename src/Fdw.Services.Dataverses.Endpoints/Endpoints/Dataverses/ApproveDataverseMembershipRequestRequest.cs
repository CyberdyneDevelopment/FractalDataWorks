using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Approves a pending membership request.</summary>
public class ApproveDataverseMembershipRequestRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the request being approved.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Gets or sets the reviewer's notes.</summary>
    public string? ReviewNotes { get; set; }
}
