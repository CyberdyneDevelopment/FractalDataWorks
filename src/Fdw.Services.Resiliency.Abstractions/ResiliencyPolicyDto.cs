using System;

namespace Fdw.Services.Resiliency.Clients.Abstractions;

/// <summary>
/// DTO representing a resiliency policy as returned by the Resiliency API client.
/// </summary>
public sealed class ResiliencyPolicyDto
{
    /// <summary>Gets or sets the policy identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the policy name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the strategy type discriminator.</summary>
    public string StrategyType { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant identifier for tenant-scoped policies.</summary>
    public Guid? TenantId { get; set; }
}
