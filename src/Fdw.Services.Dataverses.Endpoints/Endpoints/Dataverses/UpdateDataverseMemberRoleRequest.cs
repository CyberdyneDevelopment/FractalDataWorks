using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Changes the role a member holds on a dataverse.</summary>
public class UpdateDataverseMemberRoleRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership being changed.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Gets or sets the role to grant: Owner, Steward, Contributor or Consumer.</summary>
    public string MemberRole { get; set; } = string.Empty;
}
