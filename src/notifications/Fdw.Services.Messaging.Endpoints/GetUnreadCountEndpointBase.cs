using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Claims.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

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
    /// <param name="logger">The logger.</param>
    protected GetUnreadCountEndpointBase(
        IMessageService messageService,
        ILogger logger)
    {
        _messageService = messageService;
        _logger = logger;
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
                if (!CallerClaims.TryGetUserId(HttpContext.User, out var userId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.GetUnreadCountNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        MessagingEndpointLog.GettingUnreadCount(_logger, userId.ToString());

        try
        {
            var result = await _messageService.GetUnreadCount(userId, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.UnreadCountFailed(_logger, userId.ToString(), result.CurrentMessage!);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(MessagingEndpointLog.GetUnreadCountFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }
}
