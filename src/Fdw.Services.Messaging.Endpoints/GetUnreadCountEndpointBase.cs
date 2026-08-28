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
/// Abstract base class for getting unread message count (GET /messages/unread-count).
/// </summary>
public abstract class GetUnreadCountEndpointBase : EndpointWithoutRequest<UnreadCountResponse>
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUnreadCountEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected GetUnreadCountEndpointBase(
        IMessageService messageService,
        ILoggerFactory loggerFactory)
    {
        _messageService = messageService;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// Gets the message service.
    /// </summary>
    protected IMessageService MessageService => _messageService;

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/messages/unread-count");
        Policies("messages:read");
        Summary(s => s.Summary = "Get unread message count");
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

        MessagingEndpointLog.GettingUnreadCount(_logger, userId.ToString());

        try
        {
            var result = await _messageService.GetUnreadCount(userId, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.UnreadCountFailed(_logger, userId.ToString(), result.CurrentMessage!);
                AddError("Failed to get unread count");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.UnreadCountRetrieved(_logger, userId.ToString(), result.Value);
            await Send.OkAsync(new UnreadCountResponse { Count = result.Value }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "get-unread-count");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
