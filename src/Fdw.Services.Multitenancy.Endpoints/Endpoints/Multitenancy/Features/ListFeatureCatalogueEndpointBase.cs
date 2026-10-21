using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the feature catalogue: <c>GET /features</c> (FDW-1580).
/// </summary>
public abstract class ListFeatureCatalogueEndpointBase : EndpointWithoutRequest<FeatureCatalogueResponse>
{
    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListFeatureCatalogueEndpointBase"/> class.</summary>
    protected ListFeatureCatalogueEndpointBase(ILogger logger)
    {
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/features");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(
            new FeatureCatalogueResponse
            {
                Features = [.. Features.All().Select(feature => new FeatureCatalogueEntry
                {
                    Name = feature.Name, DisplayName = feature.DisplayName, Description = feature.Description,
                })],
            }, ct).ConfigureAwait(false);
    }
}
