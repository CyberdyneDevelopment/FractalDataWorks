using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for listing access requests (GET /access-requests).
/// Admins see all pending requests; users see their own.
/// </summary>
public abstract class ListAccessRequestsEndpointBase : EndpointWithoutRequest<IReadOnlyList<AccessRequestPayload>>
{
    private readonly IAccessRequestService _accessRequestService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAccessRequestsEndpointBase"/> class.
    /// </summary>
    /// <param name="accessRequestService">The access request service.</param>
    /// <param name="logger">The logger.</param>
    protected ListAccessRequestsEndpointBase(
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
        Get("/access-requests");
        Policies("access-requests:read");
        Summary(s => s.Summary = "List access requests");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
                if (!CallerClaims.TryGetUserId(HttpContext.User, out var userId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.ListAccessRequestsNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        MessagingEndpointLog.ListingAccessRequests(_logger, userId.ToString());

        try
        {
            var hasManage = HttpContext.User.HasClaim("permission", "access-requests:manage");

            IReadOnlyList<AccessRequestPayload> requests;

            if (hasManage)
            {
                var tenantIdClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
                Guid? tenantId = null;
                if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tid))
                {
                    tenantId = tid;
                }

                var result = await _accessRequestService.GetPending(tenantId, ct).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    MessagingEndpointLog.AccessRequestListFailed(_logger, result.CurrentMessage!);
                    await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
                    return;
                }

                requests = result.Value!;
            }
            else
            {
                var result = await _accessRequestService.GetForUser(userId, ct).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    MessagingEndpointLog.AccessRequestListFailed(_logger, result.CurrentMessage!);
                    await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
                    return;
                }

                requests = result.Value!;
            }

            MessagingEndpointLog.AccessRequestsListed(_logger, requests.Count);
            await Send.OkAsync(requests, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "list-access-requests");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.ListAccessRequestsFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }
}
