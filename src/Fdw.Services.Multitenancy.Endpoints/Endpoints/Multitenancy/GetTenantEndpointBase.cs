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
/// Generic base endpoint for getting a tenant by ID.
/// </summary>
public abstract class GetTenantEndpointBase : Endpoint<GetTenantRequest, TenantDto>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected GetTenantEndpointBase(ITenantProvider tenantProvider, ISystemRoleConfiguration systemRoleConfiguration)
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
        Get("/tenants/{Name}");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(GetTenantRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        IGenericResult<ITenant> tenantResult;
        if (Guid.TryParse(req.Name, out var parsedId))
        {
            tenantResult = await _tenantProvider.GetTenant(parsedId, ct).ConfigureAwait(false);
        }
        else
        {
            tenantResult = await _tenantProvider.GetTenantBySlug(req.Name, ct).ConfigureAwait(false);
            if (!tenantResult.IsSuccess || tenantResult.Value is null)
            {
                var lower = req.Name.ToLowerInvariant();
                if (!string.Equals(lower, req.Name, StringComparison.Ordinal))
                {
                    tenantResult = await _tenantProvider.GetTenantBySlug(lower, ct).ConfigureAwait(false);
                }
            }
        }

        if (!tenantResult.IsSuccess || tenantResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var tenant = tenantResult.Value;
        var isAdmin = _systemRoleConfiguration.IsInRole(User, _systemRoleConfiguration.AdminRoleName);
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!isAdmin && !string.IsNullOrEmpty(userId))
        {
            var hasAccess = await ValidateTenantAccess(tenant.Id, userId, ct).ConfigureAwait(false);
            if (!hasAccess)
            {
                OnAccessDenied(userId, tenant.Id);
                await Send.ForbiddenAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var dto = MapTenant(tenant, isAdmin);
        await Send.OkAsync(dto, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates whether the user has access to the tenant.
    /// Override to customize access validation logic.
    /// </summary>
    protected virtual async Task<bool> ValidateTenantAccess(Guid tenantId, string userId, CancellationToken ct)
    {
        var result = await _tenantProvider.ValidateTenantAccess(tenantId, userId, ct).ConfigureAwait(false);
        return result.IsSuccess && result.Value;
    }

    /// <summary>
    /// Maps a tenant entity to a DTO.
    /// Override to customize mapping.
    /// </summary>
    protected virtual TenantDto MapTenant(ITenant tenant, bool isAdmin)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            IsActive = tenant.IsActive,
            ConnectionName = isAdmin ? tenant.ConnectionName : null,
            Theme = TenantThemeDto.FromTheme(tenant.Theme),
            AvailableRoles = tenant.AvailableRoles.ToList()
        };
    }

    /// <summary>
    /// Called when access is denied. Override for custom logging.
    /// </summary>
    protected virtual void OnAccessDenied(string userId, Guid tenantId)
    {
    }
}
