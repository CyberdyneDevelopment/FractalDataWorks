using System;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A person in an organization and their role (FDW-1593).</summary>
public class OrganizationMemberResponse
{
    /// <summary>Gets or sets the person.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the role they hold in the organization.</summary>
    public string RoleName { get; set; } = string.Empty;
}
