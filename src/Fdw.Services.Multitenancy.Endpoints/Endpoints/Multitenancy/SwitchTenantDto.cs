using System;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Response for tenant switch operation.
/// </summary>
public class SwitchTenantDto
{
    /// <summary>
    /// Gets or sets whether the switch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the new access token with updated tenant claim.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>When the access token expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the tenant information.
    /// </summary>
    public TenantDto? Tenant { get; set; }
}
