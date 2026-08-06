namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to list tenants with optional filtering.
/// </summary>
public class ListTenantsRequest
{
    /// <summary>
    /// Gets or sets whether to include inactive tenants.
    /// </summary>
    public bool IncludeInactive { get; set; }
}
