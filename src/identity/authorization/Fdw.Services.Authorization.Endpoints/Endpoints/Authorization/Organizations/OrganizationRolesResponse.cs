using System.Collections.Generic;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>The roles of an organization (FDW-1593).</summary>
public class OrganizationRolesResponse
{
    /// <summary>Gets or sets the roles by name.</summary>
    public IList<OrganizationRoleResponse> Roles { get; set; } = [];
}
