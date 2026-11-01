using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for assigning an organization one of its tenant's grants for a purpose (FDW-1581). The caller must hold the right
/// at the organization's scope; the grant must be the organization's tenant's.
/// </summary>
public abstract class AssignOrganizationConnectionEndpointBase : Endpoint<OrganizationConnectionRequest, OrganizationConnectionResponse>
{
    private readonly IConnectionAssignments _assignments;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="AssignOrganizationConnectionEndpointBase"/> class.</summary>
    protected AssignOrganizationConnectionEndpointBase(ILogger logger, IConnectionAssignments assignments, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _assignments = assignments;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/organizations/{OrganizationId}/connections/{Purpose}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationConnectionRequest req, CancellationToken ct)
    {
        var assigned = await _assignments.Assign(req.OrganizationId, req.Purpose, req.GrantId, req.BaseFolder, ct).ConfigureAwait(false);
        if (assigned.IsFailure || assigned.Value is null)
        {
            await Send.ResultAsync(ConnectionStoreProblem.Of(assigned, HttpContext)).ConfigureAwait(false);
            return;
        }

        var assignment = assigned.Value;
        var audited = await ConnectionStoreAudit.Assignment(_audit, "ConnectionAssigned", req.OrganizationId, req.Purpose, null,
            ConnectionStoreAudit.Describe(assignment.GrantId, assignment.ConnectionName, assignment.FolderPath, assignment.BaseFolder), ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            ConnectionStoreEndpointLog.ChangeNotAudited(EndpointLogger, "assignment", $"{req.OrganizationId}:{req.Purpose}", audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new OrganizationConnectionResponse
        {
            OrganizationId = assignment.OrganizationId, Purpose = assignment.Purpose, GrantId = assignment.GrantId, ConnectionName = assignment.ConnectionName,
            FolderPath = assignment.FolderPath, BaseFolder = assignment.BaseFolder,
        }, ct).ConfigureAwait(false);
    }
}
