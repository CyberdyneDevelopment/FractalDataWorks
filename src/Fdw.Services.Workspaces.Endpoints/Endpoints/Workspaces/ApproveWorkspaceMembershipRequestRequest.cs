using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Approves a pending membership request.</summary>
public class ApproveWorkspaceMembershipRequestRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the request being approved.</summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the role the reviewer grants. Required: the reviewer decides it, whatever was asked
    /// for, and may grant at most their own rank (FDW-1070).
    /// </summary>
    public string GrantedRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the reviewer's notes.</summary>
    public string? ReviewNotes { get; set; }
}
