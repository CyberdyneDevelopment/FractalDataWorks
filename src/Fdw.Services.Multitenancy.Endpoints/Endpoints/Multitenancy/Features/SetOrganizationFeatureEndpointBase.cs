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
/// Generic base endpoint for switching a feature on or off for an organization:
/// <c>PUT /organizations/{OrganizationId}/features/{Feature}</c> (FDW-1580). The tenant of the organization may, and a platform
/// administrator; a feature the platform has not switched on for the tenant cannot be switched on for its organizations. The switch is
/// recorded in the administrative audit at the organization level, naming the organization.
/// </summary>
public abstract class SetOrganizationFeatureEndpointBase : Endpoint<SetOrganizationFeatureRequest, FeatureResponse>
{
    private readonly IFeatureEnablement _features;
    private readonly IOrganizationProvider _organizations;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SetOrganizationFeatureEndpointBase"/> class.</summary>
    protected SetOrganizationFeatureEndpointBase(
        ILogger logger, IFeatureEnablement features, IOrganizationProvider organizations, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _features = features;
        _organizations = organizations;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/organizations/{OrganizationId}/features/{Feature}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetOrganizationFeatureRequest req, CancellationToken ct)
    {
        var organization = await _organizations.Get(req.OrganizationId, ct).ConfigureAwait(false);
        if (organization.IsFailure || organization.Value is null)
        {
            await Send.ResultAsync(FeatureProblem.Of(organization, HttpContext)).ConfigureAwait(false);
            return;
        }

        var tenantId = organization.Value.TenantId;
        if (!FeatureCaller.MayActFor(User, tenantId, _systemRoles))
        {
            FeatureEndpointLog.OtherTenant(EndpointLogger, tenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var set = await _features.SetForOrganization(tenantId, req.OrganizationId, req.Feature, req.Enabled, ct).ConfigureAwait(false);
        if (set.IsFailure || set.Value is null)
        {
            await Send.ResultAsync(FeatureProblem.Of(set, HttpContext)).ConfigureAwait(false);
            return;
        }

        var itemId = $"{req.OrganizationId}:{set.Value.Name}";
        var audited = await FeatureAudit.Record(_audit, req.Enabled, FeatureState.OrganizationLevel, itemId, req.OrganizationId, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            FeatureEndpointLog.SwitchNotAudited(EndpointLogger, set.Value.Name, itemId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(FeatureMap.Of(set.Value), ct).ConfigureAwait(false);
    }
}
