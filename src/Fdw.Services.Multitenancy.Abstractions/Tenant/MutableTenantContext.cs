using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Request-scoped tenant context implementation.
/// </summary>
public sealed class MutableTenantContext : IMutableTenantContext
{
    private ITenant? _currentTenant;

    /// <inheritdoc />
    public ITenant? CurrentTenant => _currentTenant;

    /// <inheritdoc />
    public Guid? TenantId => _currentTenant?.Id;

    /// <inheritdoc />
    public string? TenantSlug => _currentTenant?.Slug;

    /// <inheritdoc />
    public bool HasTenant => _currentTenant != null;

    /// <inheritdoc />
    public bool IsGlobalTenant => _currentTenant?.IsGlobal ?? false;

    /// <inheritdoc />
    public string? ConnectionName => _currentTenant?.ConnectionName;

    /// <inheritdoc />
    public ITenantTheme? Theme => _currentTenant?.Theme;

    /// <inheritdoc />
    public ITenantOptions? Options => _currentTenant?.Options;

    /// <inheritdoc />
    public void SetTenant(ITenant tenant)
    {
        _currentTenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }

    /// <inheritdoc />
    public void Clear()
    {
        _currentTenant = null;
    }
}
