using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for switching a feature on or off for a tenant: <c>PUT /tenants/{TenantId}/features/{Feature}</c> (FDW-1580).
/// Only a platform administrator may; the switch is recorded in the administrative audit at the platform level.
/// </summary>
public abstract class SetTenantFeatureEndpointBase : Endpoint<SetTenantFeatureRequest, FeatureResponse>
{
    private readonly IFeatureEnablement _features;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SetTenantFeatureEndpointBase"/> class.</summary>
    protected SetTenantFeatureEndpointBase(ILogger logger, IFeatureEnablement features, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _features = features;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/tenants/{TenantId}/features/{Feature}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetTenantFeatureRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            FeatureEndpointLog.PlatformAdministratorRequired(EndpointLogger, req.Feature, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var set = await _features.SetForTenant(req.TenantId, req.Feature, req.Enabled, ct).ConfigureAwait(false);
        if (set.IsFailure || set.Value is null)
        {
            await Send.ResultAsync(FeatureProblem.Of(set, HttpContext)).ConfigureAwait(false);
            return;
        }

        var itemId = $"{req.TenantId}:{set.Value.Name}";
        var audited = await FeatureAudit.Record(_audit, req.Enabled, FeatureState.PlatformLevel, itemId, null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            FeatureEndpointLog.SwitchNotAudited(EndpointLogger, set.Value.Name, itemId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(FeatureMap.Of(set.Value), ct).ConfigureAwait(false);
    }
}
