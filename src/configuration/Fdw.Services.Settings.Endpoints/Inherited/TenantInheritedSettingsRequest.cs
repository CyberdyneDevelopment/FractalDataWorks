using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request naming a tenant whose settings are read (FDW-1579).
/// </summary>
public class TenantInheritedSettingsRequest
{
    /// <summary>Gets or sets the tenant.</summary>
    public Guid TenantId { get; set; }
}
