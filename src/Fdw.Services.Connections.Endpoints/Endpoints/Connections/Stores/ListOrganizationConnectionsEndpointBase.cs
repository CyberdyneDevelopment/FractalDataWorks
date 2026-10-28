using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for listing the connections assigned to an organization (FDW-1581). The caller must hold the right at the organization's scope.
/// </summary>
public abstract class ListOrganizationConnectionsEndpointBase : Endpoint<OrganizationConnectionsRequest, OrganizationConnectionsResponse>
{
    private readonly IConnectionAssignments _assignments;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationConnectionsEndpointBase"/> class.</summary>
    protected ListOrganizationConnectionsEndpointBase(ILogger logger, IConnectionAssignments assignments)
    {
        EndpointLogger = logger;
        _assignments = assignments;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/connections");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationConnectionsRequest req, CancellationToken ct)
    {
        var listed = await _assignments.List(req.OrganizationId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(ConnectionStoreProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new OrganizationConnectionsResponse
        {
            Connections = [.. listed.Value.Select(assignment => new OrganizationConnectionResponse
            {
                OrganizationId = assignment.OrganizationId, Purpose = assignment.Purpose, GrantId = assignment.GrantId, ConnectionName = assignment.ConnectionName,
                FolderPath = assignment.FolderPath, BaseFolder = assignment.BaseFolder,
            })],
        }, ct).ConfigureAwait(false);
    }
}
