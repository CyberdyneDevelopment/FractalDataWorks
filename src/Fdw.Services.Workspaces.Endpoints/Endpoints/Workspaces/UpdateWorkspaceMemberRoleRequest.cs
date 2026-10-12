using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Changes the role a member holds on a workspace.</summary>
public class UpdateWorkspaceMemberRoleRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership being changed.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Gets or sets the role to grant: Owner, Steward, Contributor or Consumer.</summary>
    public string MemberRole { get; set; } = string.Empty;
}
