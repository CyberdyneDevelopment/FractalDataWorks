using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for taking a connection back from a tenant (FDW-1581). Only a platform administrator may, and not while an
/// organization is assigned to the grant.
/// </summary>
public abstract class RevokeConnectionGrantEndpointBase : Endpoint<ConnectionGrantKeyRequest>
{
    private readonly IConnectionGrants _grants;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="RevokeConnectionGrantEndpointBase"/> class.</summary>
    protected RevokeConnectionGrantEndpointBase(ILogger logger, IConnectionGrants grants, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _grants = grants;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/tenants/{TenantId}/connection-grants/{Purpose}/{ConnectionName}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ConnectionGrantKeyRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            ConnectionStoreEndpointLog.PlatformAdministratorRequired(EndpointLogger, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var revoked = await _grants.Revoke(req.TenantId, req.ConnectionName, req.Purpose, ct).ConfigureAwait(false);
        if (revoked.IsFailure || revoked.Value is null)
        {
            await Send.ResultAsync(ConnectionStoreProblem.Of(revoked, HttpContext)).ConfigureAwait(false);
            return;
        }

        var grant = revoked.Value;
        var audited = await ConnectionStoreAudit.Grant(_audit, "ConnectionRevoked", grant.TenantId, grant.ConnectionName, grant.Purpose,
            ConnectionStoreAudit.Describe(grant.Id, grant.ConnectionName, grant.FolderPath, null), null, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            ConnectionStoreEndpointLog.ChangeNotAudited(EndpointLogger, "revocation", $"{grant.TenantId}:{grant.Purpose}:{grant.ConnectionName}", audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
