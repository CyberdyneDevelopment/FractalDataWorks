using System;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Null-object tenant context — used when multitenancy is not wired in the DI container.
/// Collapses the 3-tier effective-permission logic to single-tier (role-catalog only).
/// </summary>
internal sealed class NullTenantContext : ITenantContext
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NullTenantContext Instance = new();

    /// <inheritdoc />
    public ITenant? CurrentTenant => null;

    /// <inheritdoc />
    public Guid? TenantId => null;

    /// <inheritdoc />
    public string? TenantSlug => null;

    /// <inheritdoc />
    public bool HasTenant => false;

    /// <inheritdoc />
    public bool IsGlobalTenant => false;

    /// <inheritdoc />
    public string? ConnectionName => null;

    /// <inheritdoc />
    public ITenantTheme? Theme => null;

    /// <inheritdoc />
    public ITenantOptions? Options => null;
}
