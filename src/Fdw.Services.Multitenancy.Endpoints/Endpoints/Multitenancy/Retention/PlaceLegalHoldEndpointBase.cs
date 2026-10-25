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
/// Generic base endpoint for putting a document under a legal hold: <c>POST /organizations/{OrganizationId}/legal-holds</c> (FDW-1575).
/// The hold stops the retention job from ending the document. It is recorded in the administrative audit at the organization level, with
/// the reason given.
/// </summary>
public abstract class PlaceLegalHoldEndpointBase : Endpoint<PlaceLegalHoldRequest, LegalHoldResponse>
{
    private readonly ILegalHolds _holds;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="PlaceLegalHoldEndpointBase"/> class.</summary>
    protected PlaceLegalHoldEndpointBase(
        ILogger logger, ILegalHolds holds, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _holds = holds;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/{OrganizationId}/legal-holds");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(PlaceLegalHoldRequest req, CancellationToken ct)
    {
        if (User.FindFirst(ClaimDefinitions.sub.Name)?.Value is not { } caller)
        {
            RetentionEndpointLog.CallerNotIdentified(EndpointLogger);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var placed = await _holds.Place(req.OrganizationId, req.DocumentId, req.Reason, caller, ct).ConfigureAwait(false);
        if (placed.IsFailure || placed.Value is null)
        {
            await Send.ResultAsync(TenantProblem.Of(placed, HttpContext)).ConfigureAwait(false);
            return;
        }

        var itemId = $"{req.OrganizationId}:{req.DocumentId}";
        var audited = await RetentionAudit.Record(_audit, "LegalHoldPlaced", "LegalHold", itemId, req.OrganizationId, null, req.DocumentId, req.Reason, ct)
            .ConfigureAwait(false);
        if (audited.IsFailure)
        {
            RetentionEndpointLog.ChangeNotAudited(EndpointLogger, "LegalHoldPlaced", itemId, audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(RetentionMap.Of(placed.Value), ct).ConfigureAwait(false);
    }
}
