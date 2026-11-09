using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Claims.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for approving an access request (POST /access-requests/{Id}/approve).
/// </summary>
public abstract class ApproveAccessRequestEndpointBase : Endpoint<ReviewAccessRequestRequest>
{
    private readonly IAccessRequestService _accessRequestService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAccessRequestEndpointBase"/> class.
    /// </summary>
    /// <param name="accessRequestService">The access request service.</param>
    /// <param name="logger">The logger.</param>
    protected ApproveAccessRequestEndpointBase(
        IAccessRequestService accessRequestService,
        ILogger logger)
    {
        _accessRequestService = accessRequestService;
        _logger = logger;
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
        Verbs(Http.POST);
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
                if (!CallerClaims.TryGetUserId(HttpContext.User, out var reviewerUserId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.ApproveAccessRequestNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        MessagingEndpointLog.ApprovingAccessRequest(_logger, req.Id.ToString());

        try
        {
            var result = await _accessRequestService.Approve(req.Id, reviewerUserId, req.Notes, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.AccessRequestApproveFailed(_logger, req.Id.ToString(), result.CurrentMessage!);
                if (FailureCategory.IsMissing(result))
                {
                    await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.ApproveAccessRequestNotFound(_logger)), HttpContext)).ConfigureAwait(false);
                    return;
                }
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.ApproveAccessRequestFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }
}
