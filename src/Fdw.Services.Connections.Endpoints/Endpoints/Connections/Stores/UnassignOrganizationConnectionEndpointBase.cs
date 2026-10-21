using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for taking the connection of a purpose away from an organization (FDW-1581). The caller must act in the organization.
/// </summary>
public abstract class UnassignOrganizationConnectionEndpointBase : Endpoint<OrganizationConnectionKeyRequest>
{
    private readonly IConnectionAssignments _assignments;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="UnassignOrganizationConnectionEndpointBase"/> class.</summary>
    protected UnassignOrganizationConnectionEndpointBase(ILogger logger, IConnectionAssignments assignments, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _assignments = assignments;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/organizations/{OrganizationId}/connections/{Purpose}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationConnectionKeyRequest req, CancellationToken ct)
    {
        var removed = await _assignments.Unassign(req.OrganizationId, req.Purpose, ct).ConfigureAwait(false);
        if (removed.IsFailure || removed.Value is null)
        {
            await Send.ResultAsync(ConnectionStoreProblem.Of(removed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var assignment = removed.Value;
        var audited = await ConnectionStoreAudit.Assignment(_audit, "ConnectionUnassigned", req.OrganizationId, req.Purpose,
            ConnectionStoreAudit.Describe(assignment.GrantId, assignment.ConnectionName, assignment.FolderPath, assignment.BaseFolder), null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            ConnectionStoreEndpointLog.ChangeNotAudited(EndpointLogger, "removal", $"{req.OrganizationId}:{req.Purpose}", audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
