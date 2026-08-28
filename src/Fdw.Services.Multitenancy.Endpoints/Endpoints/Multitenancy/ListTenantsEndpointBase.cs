using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing tenants.
/// </summary>
public abstract class ListTenantsEndpointBase : Endpoint<ListTenantsRequest, List<TenantDto>>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected ListTenantsEndpointBase(ITenantProvider tenantProvider, ISystemRoleConfiguration systemRoleConfiguration)
    {
        _tenantProvider = tenantProvider;
        _systemRoleConfiguration = systemRoleConfiguration;
    }

    /// <summary>
    /// Gets the tenant provider.
    /// </summary>
    protected ITenantProvider TenantProvider => _tenantProvider;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/tenants");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ListTenantsRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var isAdmin = _systemRoleConfiguration.IsInRole(User, _systemRoleConfiguration.AdminRoleName);
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var tenantsResult = await GetTenants(userId, isAdmin, req.IncludeInactive, ct).ConfigureAwait(false);

        if (!tenantsResult.IsSuccess || tenantsResult.Value is null)
        {
            await Send.OkAsync(new List<TenantDto>(), ct).ConfigureAwait(false);
            return;
        }

        var defaultTenantId = !isAdmin && !string.IsNullOrEmpty(userId)
            ? await GetDefaultTenantId(userId, ct).ConfigureAwait(false)
            : null;

        var tenants = tenantsResult.Value
            .Select(t => MapTenant(t, isAdmin, defaultTenantId))
            .ToList();

        await Send.OkAsync(tenants, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the list of tenants. Override to customize tenant filtering.
    /// </summary>
    protected virtual async Task<IGenericResult<IEnumerable<ITenant>>> GetTenants(
        string? userId,
        bool isAdmin,
        bool includeInactive,
        CancellationToken ct)
    {
        if (isAdmin)
        {
            return includeInactive
                ? await _tenantProvider.GetAllTenants(ct).ConfigureAwait(false)
                : await _tenantProvider.GetActiveTenants(ct).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(userId))
        {
            return GenericResult<IEnumerable<ITenant>>.Success(new List<ITenant>());
        }

        // Non-admin users get empty list by default.
        // Override this method to inject UserTenantConfigurationProvider and call GetUserTenants.
        return GenericResult<IEnumerable<ITenant>>.Success(Array.Empty<ITenant>());
    }

    /// <summary>
    /// Gets the user's default tenant identifier for populating <see cref="TenantDto.IsDefault"/>.
    /// Returns <c>null</c> if the store is not available or the user has no default.
    /// Override to inject <c>UserTenantConfigurationProvider</c> and resolve the real default.
    /// </summary>
    protected virtual Task<Guid?> GetDefaultTenantId(string userId, CancellationToken ct)
        => Task.FromResult<Guid?>(null);

    /// <summary>
    /// Maps a tenant entity to a DTO. Override to customize mapping.
    /// </summary>
    protected virtual TenantDto MapTenant(ITenant tenant, bool isAdmin, Guid? defaultTenantId = null)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            IsActive = tenant.IsActive,
            IsDefault = defaultTenantId.HasValue && defaultTenantId.Value == tenant.Id,
            ConnectionName = isAdmin ? tenant.ConnectionName : null,
            Theme = TenantThemeDto.FromTheme(tenant.Theme),
            AvailableRoles = tenant.AvailableRoles.ToList()
        };
    }
}
