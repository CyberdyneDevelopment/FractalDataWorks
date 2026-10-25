using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Generic base endpoint for lifting the legal hold on a document: <c>POST /organizations/{OrganizationId}/legal-holds/lift</c> (FDW-1575).
/// The document id is in the body because the application's ids may hold slashes. The release is recorded in the administrative audit at
/// the organization level; the hold row stays as the record of who held the document and who released it.
/// </summary>
public abstract class LiftLegalHoldEndpointBase : Endpoint<LiftLegalHoldRequest, LegalHoldResponse>
{
    private readonly ILegalHolds _holds;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="LiftLegalHoldEndpointBase"/> class.</summary>
    protected LiftLegalHoldEndpointBase(
        ILogger logger, ILegalHolds holds, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _holds = holds;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/{OrganizationId}/legal-holds/lift");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(LiftLegalHoldRequest req, CancellationToken ct)
    {
        if (User.FindFirst(ClaimDefinitions.sub.Name)?.Value is not { } caller)
        {
            RetentionEndpointLog.CallerNotIdentified(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var lifted = await _holds.Lift(req.OrganizationId, req.DocumentId, caller, ct).ConfigureAwait(false);
        if (lifted.IsFailure || lifted.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(lifted, HttpContext)).ConfigureAwait(false);
            return;
        }

        var itemId = $"{req.OrganizationId}:{req.DocumentId}";
        var audited = await RetentionAudit.Record(_audit, "LegalHoldLifted", "LegalHold", itemId, req.OrganizationId, req.DocumentId, null, null, ct)
            .ConfigureAwait(false);
        if (audited.IsFailure)
        {
            RetentionEndpointLog.ChangeNotAudited(EndpointLogger, "LegalHoldLifted", itemId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(lifted.Value), ct).ConfigureAwait(false);
    }
}
