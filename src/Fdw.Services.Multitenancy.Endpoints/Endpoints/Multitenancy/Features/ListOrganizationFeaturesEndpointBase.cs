using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the features as they stand for an organization: <c>GET /organizations/{OrganizationId}/features</c>
/// (FDW-1580). A platform administrator reads any organization's; anyone else reads those of the tenant they act in.
/// </summary>
public abstract class ListOrganizationFeaturesEndpointBase : Endpoint<OrganizationFeaturesRequest, FeaturesResponse>
{
    private readonly IFeatureEnablement _features;
    private readonly IOrganizationProvider _organizations;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationFeaturesEndpointBase"/> class.</summary>
    protected ListOrganizationFeaturesEndpointBase(
        ILogger logger, IFeatureEnablement features, IOrganizationProvider organizations, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _features = features;
        _organizations = organizations;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/features");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationFeaturesRequest req, CancellationToken ct)
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

        var listed = await _features.ForOrganization(tenantId, req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(FeatureProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(FeatureMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
