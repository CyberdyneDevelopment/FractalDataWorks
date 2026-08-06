using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for getting the current authenticated user's tenant.
/// </summary>
public abstract class GetCurrentTenantEndpointBase : EndpointWithoutRequest<TenantDto>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ISystemRoleConfiguration _systemRoleConfiguration;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected GetCurrentTenantEndpointBase(ITenantProvider tenantProvider, ISystemRoleConfiguration systemRoleConfiguration)
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
        Get("/tenants/current");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var tenantIdClaim = User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            OnNoTenantClaim();
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var tenantResult = await _tenantProvider.GetTenant(tenantId, ct).ConfigureAwait(false);

        if (!tenantResult.IsSuccess || tenantResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var isAdmin = _systemRoleConfiguration.IsInRole(User, _systemRoleConfiguration.AdminRoleName);
        var tenant = tenantResult.Value;
        var dto = MapTenant(tenant, isAdmin);

        await Send.OkAsync(dto, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a tenant entity to a DTO. Override to customize mapping.
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
    /// Called when the user has no tenant claim. Override for custom logging.
    /// </summary>
    protected virtual void OnNoTenantClaim()
    {
    }
}
