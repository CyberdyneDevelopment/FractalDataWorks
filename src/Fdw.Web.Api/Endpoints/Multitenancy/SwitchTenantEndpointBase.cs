using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Multitenancy.Clients.Models;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for switching the current user's active tenant.
/// </summary>
public abstract class SwitchTenantEndpointBase : Endpoint<SwitchTenantRequest, SwitchTenantDto>
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    protected SwitchTenantEndpointBase(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Gets the tenant provider.
    /// </summary>
    protected ITenantProvider TenantProvider => _tenantProvider;

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tenants/switch");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (auth, summary, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SwitchTenantRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var hasAccess = await _tenantProvider.ValidateTenantAccess(req.TenantId, userId, ct).ConfigureAwait(false);
        if (!hasAccess.IsSuccess || !hasAccess.Value)
        {
            OnAccessDenied(userId, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var tenantResult = await _tenantProvider.GetTenant(req.TenantId, ct).ConfigureAwait(false);
        if (!tenantResult.IsSuccess || tenantResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var tenant = tenantResult.Value;

        var tokenResult = await GenerateNewTokens(userId, tenant, ct).ConfigureAwait(false);
        if (!tokenResult.IsSuccess)
        {
            await Send.ResponseAsync(new SwitchTenantDto
            {
                Success = false,
                Message = "Failed to generate new tokens"
            }, 500, ct).ConfigureAwait(false);
            return;
        }

        OnTenantSwitched(userId, req.TenantId);

        var response = new SwitchTenantDto
        {
            Success = true,
            AccessToken = tokenResult.Value.AccessToken,
            RefreshToken = tokenResult.Value.RefreshToken,
            ExpiresIn = tokenResult.Value.ExpiresIn,
            Message = "Tenant switched successfully",
            Tenant = MapTenant(tenant)
        };

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates new JWT tokens with updated tenant claim.
    /// Override to customize token generation.
    /// </summary>
    protected abstract Task<IGenericResult<(string AccessToken, string RefreshToken, int ExpiresIn)>> GenerateNewTokens(
        string userId,
        ITenant tenant,
        CancellationToken ct);

    /// <summary>
    /// Maps a tenant entity to a DTO. Override to customize mapping.
    /// </summary>
    protected virtual TenantDto MapTenant(ITenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            IsActive = tenant.IsActive,
            ConnectionName = null,
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

    /// <summary>
    /// Called when tenant switch succeeds. Override for custom logging.
    /// </summary>
    protected virtual void OnTenantSwitched(string userId, Guid tenantId)
    {
    }
}
