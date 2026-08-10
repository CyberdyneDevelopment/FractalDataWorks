namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Request to get a role by name.
/// </summary>
public class GetRoleRequest
{
    /// <summary>
    /// Gets or sets the role name (bound from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
