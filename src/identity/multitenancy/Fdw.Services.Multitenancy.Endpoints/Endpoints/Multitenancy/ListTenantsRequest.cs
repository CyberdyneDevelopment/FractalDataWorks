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

    /// <summary>
    /// Gets or sets whether to also list deleted tenants, each marked <see cref="Fdw.Services.Multitenancy.Abstractions.TenantDto.IsDeleted"/>.
    /// Only a platform administrator may ask.
    /// </summary>
    public bool IncludeDeleted { get; set; }
}
