namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request naming a tenant whose features are read (FDW-1580).
/// </summary>
public class TenantFeaturesRequest
{
    /// <summary>Gets or sets the tenant.</summary>
    public System.Guid TenantId { get; set; }
}
