using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the legal holds of an organization:
/// <c>GET /organizations/{OrganizationId}/legal-holds</c> (FDW-1575). <c>heldOnly=true</c> leaves out the holds already lifted.
/// </summary>
public abstract class ListLegalHoldsEndpointBase : Endpoint<ListLegalHoldsRequest, LegalHoldsResponse>
{
    private readonly ILegalHolds _holds;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListLegalHoldsEndpointBase"/> class.</summary>
    protected ListLegalHoldsEndpointBase(ILogger logger, ILegalHolds holds)
    {
        EndpointLogger = logger;
        _holds = holds;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/legal-holds");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ListLegalHoldsRequest req, CancellationToken ct)
    {
        var listed = await _holds.List(req.OrganizationId, req.HeldOnly, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
