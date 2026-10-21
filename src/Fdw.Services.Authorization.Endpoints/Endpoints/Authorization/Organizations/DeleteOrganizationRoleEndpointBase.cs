using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a role of an organization that nobody holds and the organization was not made with (FDW-1593).
/// </summary>
public abstract class DeleteOrganizationRoleEndpointBase : Endpoint<OrganizationRoleKeyRequest>
{
    private readonly IOrganizationRoles _roles;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="DeleteOrganizationRoleEndpointBase"/> class.</summary>
    protected DeleteOrganizationRoleEndpointBase(ILogger logger, IOrganizationRoles roles, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _roles = roles;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/organizations/{OrganizationId}/roles/{RoleName}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRoleKeyRequest req, CancellationToken ct)
    {
        var before = await _roles.Get(req.OrganizationId, req.RoleName, ct).ConfigureAwait(false);
        if (before.IsFailure || before.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(before, HttpContext)).ConfigureAwait(false);
            return;
        }

        var deleted = await _roles.Delete(req.OrganizationId, req.RoleName, ct).ConfigureAwait(false);
        if (deleted.IsFailure)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(deleted, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await OrganizationChangeAudit.Role(_audit, "OrganizationRoleDeleted", req.OrganizationId, before.Value.Name,
            OrganizationChangeAudit.Describe(before.Value.Description, before.Value.Permissions), null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.OrganizationChangeNotAudited(EndpointLogger, "deletion", before.Value.Name, req.OrganizationId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
