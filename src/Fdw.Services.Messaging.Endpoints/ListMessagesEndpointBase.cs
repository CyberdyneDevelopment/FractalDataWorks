using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for listing messages for the current user (GET /messages).
/// </summary>
public abstract class ListMessagesEndpointBase : Endpoint<ListMessagesRequest, IReadOnlyList<MessagePayload>>
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListMessagesEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected ListMessagesEndpointBase(
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
        Get("/messages");
        Policies("messages:read");
        Summary(s => s.Summary = "List messages for the current user");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListMessagesRequest req, CancellationToken ct)
    {
        var userIdClaim = HttpContext.User.FindFirst("sub")?.Value
            ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            MessagingEndpointLog.UserClaimNotFound(_logger);
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        MessagingEndpointLog.ListingMessages(_logger, userId.ToString());

        try
        {
            var query = new MessageQuery
            {
                UserId = userId,
                MessageType = req.MessageType,
                Severity = req.Severity,
                Status = req.Status,
                ReferenceId = req.ReferenceId,
                After = req.After,
                Before = req.Before,
                Skip = req.Skip,
                Take = req.Take > 0 ? req.Take : 50
            };

            var result = await _messageService.GetMessages(query, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.MessageListFailed(_logger, userId.ToString(), result.CurrentMessage!);
                AddError("Failed to list messages");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.MessagesListed(_logger, result.Value!.Count, userId.ToString());
            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "list-messages");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
