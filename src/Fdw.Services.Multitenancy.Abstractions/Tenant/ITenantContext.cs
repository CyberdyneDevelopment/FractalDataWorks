using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Provides access to the current tenant context.
/// Scoped per-request in web scenarios.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant, or null if not in a tenant context.
    /// </summary>
    ITenant? CurrentTenant { get; }

    /// <summary>
    /// Gets the current tenant ID, or null if not in a tenant context.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the current tenant slug, or null if not in a tenant context.
    /// </summary>
    string? TenantSlug { get; }

    /// <summary>
    /// Gets whether a tenant context is active.
    /// </summary>
    bool HasTenant { get; }

    /// <summary>
    /// Gets whether the current tenant is the global/home tenant.
    /// False when there is no active tenant context.
    /// </summary>
    bool IsGlobalTenant { get; }

    /// <summary>
    /// Gets the tenant's connection name override, if any.
    /// </summary>
    string? ConnectionName { get; }

    /// <summary>
    /// Gets the tenant's theme.
    /// </summary>
    ITenantTheme? Theme { get; }

    /// <summary>
    /// Gets the tenant's options.
    /// </summary>
    ITenantOptions? Options { get; }
}
