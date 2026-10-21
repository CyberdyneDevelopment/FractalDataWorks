namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A request to give a person a role in an organization (FDW-1593).</summary>
public class OrganizationMemberRequest : OrganizationMemberKeyRequest
{
    /// <summary>Gets or sets the role to give.</summary>
    public string RoleName { get; set; } = string.Empty;
}
