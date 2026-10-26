namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to get a tenant by ID or by slug/name (bound from route).
/// </summary>
public class GetTenantRequest
{
    /// <summary>
    /// Gets or sets the tenant identifier (route segment {Id}). Resolved as a Guid first;
    /// if not a Guid the value is treated as the tenant slug. It is named Id so that every
    /// operation on /tenants/{Id} declares one path parameter name in the OpenAPI document.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}
