namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request to get a role by name.
/// </summary>
public class GetRoleRequest
{
    /// <summary>
    /// Gets or sets the role name: the body of <c>POST /roles/get</c>, or the route value for the
    /// role sub-resources (<c>DELETE /roles/{Name}</c>, <c>GET /roles/{Name}/permissions</c>).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
