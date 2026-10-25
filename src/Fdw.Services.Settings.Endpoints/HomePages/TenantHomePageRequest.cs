using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request naming a tenant whose home page is read or published (FDW-2471).
/// </summary>
public class TenantHomePageRequest
{
    /// <summary>Gets or sets the tenant.</summary>
    public Guid TenantId { get; set; }
}
