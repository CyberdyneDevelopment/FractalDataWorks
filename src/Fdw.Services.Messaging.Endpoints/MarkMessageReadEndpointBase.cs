using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for marking a message as read (PUT /messages/{Id}/read).
/// </summary>
public abstract class MarkMessageReadEndpointBase : Endpoint<MessageIdRequest>
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkMessageReadEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected MarkMessageReadEndpointBase(
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
        Put("/messages/{Id}/read");
        Policies("messages:read");
        Summary(s => s.Summary = "Mark a message as read");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(MessageIdRequest req, CancellationToken ct)
    {
        MessagingEndpointLog.MarkingMessageRead(_logger, req.Id.ToString());

        try
        {
            var result = await _messageService.MarkRead(req.Id, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.MarkReadFailed(_logger, req.Id.ToString(), result.CurrentMessage!);
                AddError("Failed to mark message as read");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.MessageMarkedRead(_logger, req.Id.ToString());
            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "mark-message-read");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
