using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Declines a pending membership request.</summary>
public class DenyWorkspaceMembershipRequestRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the request being declined.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Gets or sets the reviewer's notes.</summary>
    public string? ReviewNotes { get; set; }
}
