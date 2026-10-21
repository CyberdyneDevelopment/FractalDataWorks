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
/// Generic base endpoint for taking a person out of an organization (FDW-1593): every access row they hold there is removed.
/// </summary>
public abstract class RemoveOrganizationMemberEndpointBase : Endpoint<OrganizationMemberKeyRequest>
{
    private readonly IOrganizationMembers _members;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="RemoveOrganizationMemberEndpointBase"/> class.</summary>
    protected RemoveOrganizationMemberEndpointBase(ILogger logger, IOrganizationMembers members, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _members = members;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/organizations/{OrganizationId}/members/{UserId}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationMemberKeyRequest req, CancellationToken ct)
    {
        var removed = await _members.Remove(req.OrganizationId, req.UserId, ct).ConfigureAwait(false);
        if (removed.IsFailure || removed.Value is null)
        {
            await Send.ResultAsync(RoleChangeRefusal.Of(removed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await OrganizationChangeAudit.Member(_audit, "OrganizationMemberRemoved", req.OrganizationId, req.UserId, removed.Value, null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            AuthorizationEndpointLog.OrganizationChangeNotAudited(EndpointLogger, "removal", req.UserId.ToString(), req.OrganizationId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
