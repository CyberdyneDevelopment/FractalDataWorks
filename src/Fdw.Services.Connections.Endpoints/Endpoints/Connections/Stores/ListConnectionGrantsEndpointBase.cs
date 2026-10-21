using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for listing the connections granted to a tenant (FDW-1581). A platform administrator reads any tenant's; anyone
/// else reads only the tenant they act in, because that is where they assign.
/// </summary>
public abstract class ListConnectionGrantsEndpointBase : Endpoint<TenantConnectionsRequest, ConnectionGrantsResponse>
{
    private readonly IConnectionGrants _grants;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListConnectionGrantsEndpointBase"/> class.</summary>
    protected ListConnectionGrantsEndpointBase(ILogger logger, IConnectionGrants grants, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _grants = grants;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/tenants/{TenantId}/connection-grants");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantConnectionsRequest req, CancellationToken ct)
    {
        var actsIn = Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var activeTenant) && activeTenant == req.TenantId;
        if (!actsIn && !_systemRoles.IsPlatformAdmin(User))
        {
            ConnectionStoreEndpointLog.OtherTenant(EndpointLogger, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var listed = await _grants.List(req.TenantId, ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(ConnectionStoreProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new ConnectionGrantsResponse
        {
            Grants = [.. listed.Value.Select(grant => new ConnectionGrantResponse
            {
                Id = grant.Id, TenantId = grant.TenantId, ConnectionName = grant.ConnectionName, Purpose = grant.Purpose, FolderPath = grant.FolderPath, MayRegisterOwn = grant.MayRegisterOwn,
            })],
        }, ct).ConfigureAwait(false);
    }
}
