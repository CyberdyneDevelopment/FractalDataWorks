using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the features as they stand for an organization: <c>GET /organizations/{OrganizationId}/features</c>
/// (FDW-1580). Who may read an organization's features is decided by the feature service (FDW-2474).
/// </summary>
public abstract class ListOrganizationFeaturesEndpointBase : Endpoint<OrganizationFeaturesRequest, FeaturesResponse>
{
    private readonly IFeatureEnablement _features;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationFeaturesEndpointBase"/> class.</summary>
    protected ListOrganizationFeaturesEndpointBase(
        ILogger logger, IFeatureEnablement features)
    {
        EndpointLogger = logger;
        _features = features;
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
        var listed = await _features.ForOrganization(req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(FeatureProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(FeatureMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
