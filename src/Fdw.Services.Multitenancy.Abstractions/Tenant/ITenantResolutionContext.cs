using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Context for tenant resolution from requests.
/// </summary>
public interface ITenantResolutionContext
{
    /// <summary>
    /// Gets the host/domain from the request.
    /// </summary>
    string? Host { get; }

    /// <summary>
    /// Gets the tenant header value if present.
    /// </summary>
    string? TenantHeader { get; }

    /// <summary>
    /// Gets the tenant slug from route if present.
    /// </summary>
    string? RouteSlug { get; }

    /// <summary>
    /// Gets the tenant ID from claims if authenticated.
    /// </summary>
    Guid? ClaimsTenantId { get; }
}
