using System;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>A member of a universe.</summary>
public class UniverseMemberDto
{
    /// <summary>Gets or sets the membership's logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the member.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the role held: Owner, Steward, Contributor or Consumer.</summary>
    public string MemberRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership state: Invited, Active, Suspended or Left.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets when the invitation was accepted.</summary>
    public DateTimeOffset? JoinedAt { get; set; }
}
