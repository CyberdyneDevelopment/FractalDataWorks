using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for creating an access request (POST /access-requests).
/// </summary>
public abstract class CreateAccessRequestEndpointBase : Endpoint<CreateAccessRequestEndpointRequest, AccessRequestPayload>
{
    private readonly IAccessRequestService _accessRequestService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccessRequestEndpointBase"/> class.
    /// </summary>
    /// <param name="accessRequestService">The access request service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected CreateAccessRequestEndpointBase(
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
        Post("/access-requests");
        // Why: self-service — any user with at least :read on access-requests can submit
        // a request to elevate their own access. The :create permission was Admin/Operator-
        // only, which prevented the Viewer role from making the very requests this endpoint
        // exists to receive.
        Policies("access-requests:read");
        Summary(s => s.Summary = "Create an access request");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <summary>
    /// Gets the endpoint type to use for CreatedAt responses.
    /// Override to specify the list endpoint type for your application.
    /// </summary>
    /// <returns>The type of the list endpoint, or null to use a 201 status without location header.</returns>
    protected virtual Type? GetListEndpointType() => null;

    /// <inheritdoc/>
    public override async Task HandleAsync(CreateAccessRequestEndpointRequest req, CancellationToken ct)
    {
        // Why: MapInboundClaims = false on JWT bearer keeps "sub" as-is; ClaimTypes.NameIdentifier
        // is the WS-Federation URI only present when claim mapping is enabled. Check "sub" first.
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var tenantIdClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            AddError("Tenant context required");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        MessagingEndpointLog.CreatingAccessRequest(_logger, req.RequestedResource, req.RequestedPermission);

        try
        {
            var request = new CreateAccessRequest
            {
                TenantId = tenantId,
                RequestingUserId = userId,
                RequestedResource = req.RequestedResource,
                RequestedPermission = req.RequestedPermission,
                Justification = req.Justification,
                ReferenceId = req.ReferenceId
            };

            var result = await _accessRequestService.RequestAccess(request, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.AccessRequestCreateFailed(_logger, result.CurrentMessage!);
                AddError("Failed to create access request");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.AccessRequestCreated(_logger, result.Value!.Id.ToString());
            await OnAccessRequestCreated(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "create-access-request");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Called when an access request is created successfully. Override to customize the response.
    /// The default sends a 201 Created response.
    /// </summary>
    /// <param name="dto">The created access request.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnAccessRequestCreated(AccessRequestPayload dto, CancellationToken ct)
    {
        return Send.ResponseAsync(dto, 201, ct);
    }
}
