using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
    /// <param name="loggerFactory">The logger factory.</param>
    protected ListAccessRequestsEndpointBase(
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
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
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
                    AddError("Failed to list access requests");
                    await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
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
                    AddError("Failed to list access requests");
                    await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
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
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
