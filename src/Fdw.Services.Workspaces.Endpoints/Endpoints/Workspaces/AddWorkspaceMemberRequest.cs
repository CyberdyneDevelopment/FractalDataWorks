using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Adds a member to a workspace.</summary>
public class AddWorkspaceMemberRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what holds the membership: User or Role.</summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the user or role to add.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>Gets or sets the role to grant: Owner, Steward, Contributor or Consumer.</summary>
    public string MemberRole { get; set; } = string.Empty;
}
