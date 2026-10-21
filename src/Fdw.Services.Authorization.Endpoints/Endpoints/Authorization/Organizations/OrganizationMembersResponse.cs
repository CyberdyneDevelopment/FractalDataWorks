using System.Collections.Generic;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>The people in an organization (FDW-1593).</summary>
public class OrganizationMembersResponse
{
    /// <summary>Gets or sets the people with their roles.</summary>
    public IList<OrganizationMemberResponse> Members { get; set; } = [];
}
