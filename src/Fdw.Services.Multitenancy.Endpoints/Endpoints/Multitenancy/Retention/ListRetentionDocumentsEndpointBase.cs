using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for listing the documents of one type that belong to an organization, so a legal hold can name one:
/// <c>GET /organizations/{OrganizationId}/retention/documents?documentType=...</c> (FDW-2906).
/// </summary>
public abstract class ListRetentionDocumentsEndpointBase : Endpoint<ListRetentionDocumentsRequest, RetentionDocumentsResponse>
{
    private readonly IRetentionCatalog _catalog;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListRetentionDocumentsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="catalog">Lists the application's documents.</param>
    protected ListRetentionDocumentsEndpointBase(ILogger logger, IRetentionCatalog catalog)
    {
        EndpointLogger = logger;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/retention/documents");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ListRetentionDocumentsRequest req, CancellationToken ct)
    {
        var listed = await _catalog.Documents(req.OrganizationId, req.DocumentType, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
