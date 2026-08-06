namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to get a tenant by ID or by slug/name (bound from route).
/// </summary>
public class GetTenantRequest
{
    /// <summary>
    /// Gets or sets the tenant identifier (route segment {Name}). Resolved as a Guid first;
    /// if not a Guid the value is treated as the tenant slug.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
