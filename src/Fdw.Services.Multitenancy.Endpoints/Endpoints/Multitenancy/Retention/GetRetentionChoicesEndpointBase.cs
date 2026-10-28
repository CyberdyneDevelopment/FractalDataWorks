using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for reading what an organization's documents offer when a retention policy is set:
/// <c>GET /organizations/{OrganizationId}/retention/choices</c> (FDW-2906).
/// </summary>
public abstract class GetRetentionChoicesEndpointBase : Endpoint<OrganizationRetentionRequest, RetentionChoicesResponse>
{
    private readonly IRetentionCatalog _catalog;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GetRetentionChoicesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="catalog">Says what the application's documents offer.</param>
    protected GetRetentionChoicesEndpointBase(ILogger logger, IRetentionCatalog catalog)
    {
        EndpointLogger = logger;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/retention/choices");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRetentionRequest req, CancellationToken ct)
    {
        var choices = await _catalog.Choices(req.OrganizationId, ct).ConfigureAwait(false);
        if (choices.IsFailure || choices.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(choices, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(choices.Value), ct).ConfigureAwait(false);
    }
}
