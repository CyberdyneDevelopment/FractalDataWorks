using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for marking all messages as read (PUT /messages/mark-all-read).
/// </summary>
public abstract class MarkAllReadEndpointBase : EndpointWithoutRequest
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllReadEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected MarkAllReadEndpointBase(
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
        Post("/messages/mark-all-read");
        Policies("messages:read");
        Summary(s => s.Summary = "Mark all messages as read");
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

        MessagingEndpointLog.MarkingAllRead(_logger, userId.ToString());

        try
        {
            var result = await _messageService.MarkAllRead(userId, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.MarkAllReadFailed(_logger, userId.ToString(), result.CurrentMessage!);
                AddError("Failed to mark all messages as read");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.AllMessagesMarkedRead(_logger, userId.ToString());
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "mark-all-read");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
