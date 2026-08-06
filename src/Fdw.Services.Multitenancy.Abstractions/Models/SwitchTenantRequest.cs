using System;

namespace Fdw.Services.Multitenancy.Clients.Models;

/// <summary>
/// Request to switch the current user's active tenant.
/// </summary>
// Why: pure request DTO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SwitchTenantRequest
{
    /// <summary>
    /// Gets or sets the target tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }
}
