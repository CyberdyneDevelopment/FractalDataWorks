using System;
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
/// Generic base endpoint for running the retention job for an organization: <c>POST /organizations/{OrganizationId}/retention/run</c>
/// (FDW-1575). The job ends the documents past their deadline that are not under a legal hold, by asking the application's
/// <see cref="IRetainedDocuments"/>, and each document ended is recorded in the administrative audit at the organization level. A
/// schedule can call this route.
/// </summary>
public abstract class RunRetentionEndpointBase : Endpoint<OrganizationRetentionRequest, RetentionRunResponse>
{
    private readonly IRetentionKeeper _keeper;
    private readonly IOrganizationProvider _organizations;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="RunRetentionEndpointBase"/> class.</summary>
    protected RunRetentionEndpointBase(
        ILogger logger, IRetentionKeeper keeper, IOrganizationProvider organizations, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _keeper = keeper;
        _organizations = organizations;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/{OrganizationId}/retention/run");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationRetentionRequest req, CancellationToken ct)
    {
        var access = await RetentionAccess.TenantOf(_organizations, _systemRoles, HttpContext, req.OrganizationId, EndpointLogger, ct).ConfigureAwait(false);
        if (access.Refusal is { } refusal)
        {
            await Send.ResultAsync(refusal).ConfigureAwait(false);
            return;
        }

        var ran = await _keeper.Run(access.TenantId, req.OrganizationId, DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
        if (ran.IsFailure || ran.Value is not { } report)
        {
            await Send.ResultAsync(TenantProblem.Of(ran, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!await AuditEnded(req.OrganizationId, report, ct).ConfigureAwait(false))
        {
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(report), ct).ConfigureAwait(false);
    }

    // Records every document the run ended, so none ended goes unrecorded when one entry fails; false when any entry could not be recorded.
    private async Task<bool> AuditEnded(Guid organizationId, RetentionRunReport report, CancellationToken cancellationToken)
    {
        var recorded = true;
        foreach (var outcome in report.Ended)
        {
            var itemId = $"{organizationId}:{outcome.DocumentId}";
            var audited = await RetentionAudit.Record(_audit, "DocumentRetentionEnded", "Document", itemId, organizationId, outcome.DocumentId, outcome.EndAction,
                $"Type '{outcome.DocumentType}' reached the end of its retention period", cancellationToken).ConfigureAwait(false);
            if (audited.IsFailure)
            {
                RetentionEndpointLog.ChangeNotAudited(EndpointLogger, "DocumentRetentionEnded", itemId, audited.CurrentMessage);
                recorded = false;
            }
        }

        return recorded;
    }
}
