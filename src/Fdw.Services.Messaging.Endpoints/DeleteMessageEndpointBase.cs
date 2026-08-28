using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for deleting a message (DELETE /messages/{Id}).
/// Why: Messaging service has no hard-delete primitive; "delete" maps to
/// <see cref="IMessageService.Dismiss"/> (user removes from their inbox).
/// </summary>
public abstract class DeleteMessageEndpointBase : Endpoint<MessageIdRequest>
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMessageEndpointBase"/> class.
    /// </summary>
    protected DeleteMessageEndpointBase(
        IMessageService messageService,
        ILoggerFactory loggerFactory)
    {
        _messageService = messageService;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>Gets the message service.</summary>
    protected IMessageService MessageService => _messageService;

    /// <summary>Gets the logger.</summary>
    protected ILogger EndpointLogger => _logger;

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/messages/{Id}");
        Policies("messages:read");
        Summary(s => s.Summary = "Delete a message");
        ConfigureEndpoint();
    }

    /// <summary>Override for endpoint-specific configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(MessageIdRequest req, CancellationToken ct)
    {
        try
        {
            var getResult = await _messageService.GetMessage(req.Id, ct).ConfigureAwait(false);
            if (!getResult.IsSuccess || getResult.Value is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var result = await _messageService.Dismiss(req.Id, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.DismissFailed(_logger, req.Id.ToString(), result.CurrentMessage!);
                AddError("Failed to delete message");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            await Send.NoContentAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "delete-message");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
