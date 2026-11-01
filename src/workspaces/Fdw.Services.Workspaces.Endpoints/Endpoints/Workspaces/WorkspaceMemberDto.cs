using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A member of a workspace.</summary>
public class WorkspaceMemberDto
{
    /// <summary>Gets or sets the membership's logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets what holds this membership: User or Role.</summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the user or role that holds it.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>Gets or sets the role held: Owner, Steward, Contributor or Consumer.</summary>
    public string MemberRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership state: Invited, Active, Suspended or Left.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets when the invitation was accepted.</summary>
    public DateTimeOffset? JoinedAt { get; set; }
}
