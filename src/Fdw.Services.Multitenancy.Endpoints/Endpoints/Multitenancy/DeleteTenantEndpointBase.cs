using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a company: <c>DELETE /tenants/{Id}</c> (FDW-1759). The company is marked deleted and disabled
/// out of every list and sign-in, but nothing it holds is removed, so a platform administrator can restore it. Only a disabled company can be
/// deleted, and never the global one.
/// </summary>
public abstract class DeleteTenantEndpointBase : Endpoint<TenantKeyRequest>
{
    private readonly ITenantWriter _writer;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="DeleteTenantEndpointBase"/> class.</summary>
    protected DeleteTenantEndpointBase(ILogger logger, ITenantWriter writer, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _writer = writer;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/tenants/{Id}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantKeyRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            TenantEndpointLog.PlatformAdministratorRequired(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var written = await _writer.SetDeleted(req.Id, true, ct).ConfigureAwait(false);
        if (written.IsFailure)
        {
            await Send.ResultAsync(TenantProblem.Of(written, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await TenantChangeAudit.Record(_audit, "TenantDeleted", req.Id, "False", "True", ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            TenantEndpointLog.ChangeNotAudited(EndpointLogger, req.Id, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        TenantEndpointLog.TenantChanged(EndpointLogger, req.Id, "TenantDeleted");
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
