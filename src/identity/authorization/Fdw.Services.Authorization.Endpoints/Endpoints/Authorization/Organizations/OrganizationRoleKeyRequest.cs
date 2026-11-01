namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A request about one role of an organization (FDW-1593).</summary>
public class OrganizationRoleKeyRequest : OrganizationRequest
{
    /// <summary>Gets or sets the role, from the route.</summary>
    public string RoleName { get; set; } = string.Empty;
}
