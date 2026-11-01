using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request naming one tenant by its identifier, for deleting and restoring it (FDW-1759).
/// </summary>
public class TenantKeyRequest
{
    /// <summary>Gets or sets the tenant.</summary>
    public Guid Id { get; set; }
}
