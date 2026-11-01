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
/// Generic base endpoint for granting a tenant a document connection for a purpose (FDW-1581). Only a platform administrator may.
/// </summary>
public abstract class GrantConnectionEndpointBase : Endpoint<ConnectionGrantRequest, ConnectionGrantResponse>
{
    private readonly IConnectionGrants _grants;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="GrantConnectionEndpointBase"/> class.</summary>
    protected GrantConnectionEndpointBase(ILogger logger, IConnectionGrants grants, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _grants = grants;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tenants/{TenantId}/connection-grants");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(ConnectionGrantRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            ConnectionStoreEndpointLog.PlatformAdministratorRequired(EndpointLogger, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var granted = await _grants.Grant(req.TenantId, req.ConnectionName, req.Purpose, req.Folder, req.MayRegisterOwn, ct).ConfigureAwait(false);
        if (granted.IsFailure || granted.Value is null)
        {
            await Send.ResultAsync(ConnectionStoreProblem.Of(granted, HttpContext)).ConfigureAwait(false);
            return;
        }

        var grant = granted.Value;
        var audited = await ConnectionStoreAudit.Grant(_audit, "ConnectionGranted", grant.TenantId, grant.ConnectionName, grant.Purpose, null,
            ConnectionStoreAudit.Describe(grant.Id, grant.ConnectionName, grant.FolderPath, null), ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            ConnectionStoreEndpointLog.ChangeNotAudited(EndpointLogger, "grant", $"{grant.TenantId}:{grant.Purpose}:{grant.ConnectionName}", audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.ResponseAsync(new ConnectionGrantResponse
        {
            Id = grant.Id, TenantId = grant.TenantId, ConnectionName = grant.ConnectionName, Purpose = grant.Purpose, FolderPath = grant.FolderPath, MayRegisterOwn = grant.MayRegisterOwn,
        }, 201, ct).ConfigureAwait(false);
    }
}
