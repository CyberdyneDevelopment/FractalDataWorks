using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Messaging.Endpoints.Models;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for listing messages for the current user (GET /messages).
/// </summary>
public abstract class ListMessagesEndpointBase : Endpoint<ListMessagesRequest, PaginatedResponse<MessagePayload>>
{
    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListMessagesEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="logger">The logger.</param>
    protected ListMessagesEndpointBase(
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

    // The order of an offset window; absent keeps the oldest-first order the list has always had, anything else but the two names is refused.
    private IGenericResult<bool> NewestFirst(string? order)
    {
        if (string.IsNullOrEmpty(order) || string.Equals(order, "oldest", StringComparison.OrdinalIgnoreCase))
            return GenericResult<bool>.Success(false);

        return string.Equals(order, "newest", StringComparison.OrdinalIgnoreCase)
            ? GenericResult<bool>.Success(true)
            : GenericResult<bool>.Failure(MessagingEndpointLog.OrderUnknown(_logger, order));
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

        var order = NewestFirst(req.Order);
        if (!order.IsSuccess)
        {
            await Send.ResultAsync(MessagingFailures.Of(order, HttpContext)).ConfigureAwait(false);
            return;
        }

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
            Take = req.Take > 0 ? req.Take : 50,
            NewestFirst = order.Value
        };

        await SendPage(query, ct).ConfigureAwait(false);
    }

    // The window the query asks for with the count of everything the filters match, as the page envelope; a refusal answers its own status.
    private async Task SendPage(MessageQuery query, CancellationToken ct)
    {
        var userId = query.UserId.ToString();
        try
        {
            var result = await _messageService.GetMessages(query, ct).ConfigureAwait(false);
            var counted = result.IsSuccess
                ? await _messageService.CountMessages(query, ct).ConfigureAwait(false)
                : result.ToNewResult<int>();

            if (!result.IsSuccess || !counted.IsSuccess || result.Value is not { } page)
            {
                IGenericResult failed = result.IsSuccess ? counted : result;
                MessagingEndpointLog.MessageListFailed(_logger, userId, failed.CurrentMessage);
                await Send.ResultAsync(MessagingFailures.Of(failed, HttpContext)).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.MessagesListed(_logger, page.Count, userId);
            await Send.OkAsync(PaginatedResponse<MessagePayload>.Create(page, query.Skip, query.Take, counted.Value), ct).ConfigureAwait(false);
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
