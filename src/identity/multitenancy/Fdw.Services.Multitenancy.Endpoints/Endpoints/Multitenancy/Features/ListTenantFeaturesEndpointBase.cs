using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the features as they stand for a tenant: <c>GET /tenants/{TenantId}/features</c> (FDW-1580). Who may
/// read a tenant's features is decided by the feature service (FDW-2478).
/// </summary>
public abstract class ListTenantFeaturesEndpointBase : Endpoint<TenantFeaturesRequest, FeaturesResponse>
{
    private readonly IFeatureEnablement _features;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListTenantFeaturesEndpointBase"/> class.</summary>
    protected ListTenantFeaturesEndpointBase(ILogger logger, IFeatureEnablement features)
    {
        EndpointLogger = logger;
        _features = features;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/tenants/{TenantId}/features");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantFeaturesRequest req, CancellationToken ct)
    {
        var listed = await _features.ForTenant(req.TenantId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(FeatureProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(FeatureMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
