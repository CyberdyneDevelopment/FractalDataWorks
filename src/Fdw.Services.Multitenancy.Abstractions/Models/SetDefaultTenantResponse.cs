using System;

namespace Fdw.Services.Multitenancy.Clients.Models;

/// <summary>
/// Response confirming the new default tenant.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SetDefaultTenantResponse
{
    /// <summary>Gets or sets the tenant identifier that was set as default.</summary>
    public Guid TenantId { get; set; }
}
