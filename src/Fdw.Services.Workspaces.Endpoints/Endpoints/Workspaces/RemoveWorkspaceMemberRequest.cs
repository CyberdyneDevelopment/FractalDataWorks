using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Removes a member from a workspace.</summary>
public class RemoveWorkspaceMemberRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership being removed.</summary>
    public Guid MemberId { get; set; }
}
