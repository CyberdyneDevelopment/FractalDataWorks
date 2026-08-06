using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for getting a single message by ID (GET /messages/{Id}).
/// </summary>
public abstract class GetMessageEndpointBase : Endpoint<MessageIdRequest, MessagePayload>
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMessageEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected GetMessageEndpointBase(
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
        Get("/messages/{Id}");
        Policies("messages:read");
        Summary(s => s.Summary = "Get a message by ID");
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
        MessagingEndpointLog.FetchingMessage(_logger, req.Id.ToString());

        try
        {
            var result = await _messageService.GetMessage(req.Id, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.MessageFetchFailed(_logger, req.Id.ToString(), result.CurrentMessage!);
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.MessageRetrieved(_logger, req.Id.ToString());
            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "get-message");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
