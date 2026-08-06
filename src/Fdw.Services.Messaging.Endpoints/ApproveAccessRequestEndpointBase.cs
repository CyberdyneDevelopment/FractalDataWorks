using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for approving an access request (PUT /access-requests/{Id}/approve).
/// </summary>
public abstract class ApproveAccessRequestEndpointBase : Endpoint<ReviewAccessRequestRequest>
{
    private readonly IAccessRequestService _accessRequestService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAccessRequestEndpointBase"/> class.
    /// </summary>
    /// <param name="accessRequestService">The access request service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected ApproveAccessRequestEndpointBase(
        IAccessRequestService accessRequestService,
        ILoggerFactory loggerFactory)
    {
        _accessRequestService = accessRequestService;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the access request service.
    /// </summary>
    protected IAccessRequestService AccessRequestService => _accessRequestService;

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        // Why: approving is an action, not idempotent state replacement — POST is the
        // canonical verb; PUT kept for backwards compatibility.
        Verbs(Http.POST, Http.PUT);
        Routes("/access-requests/{Id}/approve");
        Policies("access-requests:manage");
        Summary(s => s.Summary = "Approve an access request");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ReviewAccessRequestRequest req, CancellationToken ct)
    {
        // Why: MapInboundClaims = false on JWT bearer keeps "sub" as-is; ClaimTypes.NameIdentifier
        // is the WS-Federation URI only present when claim mapping is enabled. Check "sub" first.
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var reviewerUserId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        MessagingEndpointLog.ApprovingAccessRequest(_logger, req.Id.ToString());

        try
        {
            var result = await _accessRequestService.Approve(req.Id, reviewerUserId, req.Notes, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.AccessRequestApproveFailed(_logger, req.Id.ToString(), result.CurrentMessage!);
                // Why: service signals missing entity via "AccessRequest not found" in the
                // current message; map that to 404 instead of a generic 500.
                if (result.CurrentMessage is not null
                    && result.CurrentMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    await Send.NotFoundAsync(ct).ConfigureAwait(false);
                    return;
                }
                AddError("Failed to approve access request");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.AccessRequestApproved(_logger, req.Id.ToString());
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "approve-access-request");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
