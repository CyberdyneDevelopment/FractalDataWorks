using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Abstract base class for sending a conversational message into a thread (POST /messages).
/// </summary>
/// <remarks>
/// The write counterpart to <see cref="ListMessagesEndpointBase"/> filtered by thread. Together they
/// are the whole transport for a conversation between a person and an agent acting on their behalf:
/// one side posts here, the other reads the thread back, and <c>MessageHub</c> pushes the arrival to
/// whichever of them is holding a live circuit.
///
/// Notifications are NOT created through this route. They are raised by the services that have
/// something to say, through the notification dispatcher, and an endpoint that let a caller mint an
/// arbitrary message type would be a way to forge one.
/// </remarks>
public abstract class SendMessageEndpointBase : Endpoint<SendMessageRequest, MessagePayload>
{
    /// <summary>The message type for a turn sent by an agent to the person it acts for.</summary>
    public const string AgentMessageType = "AgentMessage";

    /// <summary>The message type for a turn sent by a person back to the agent.</summary>
    public const string UserReplyType = "UserReply";

    private readonly IMessageService _messageService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageEndpointBase"/> class.
    /// </summary>
    /// <param name="messageService">The message service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    protected SendMessageEndpointBase(
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
        Post("/messages");
        Policies("messages:write");
        Summary(s => s.Summary = "Send a message into a conversation thread");
        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <summary>
    /// Checks the parts of the request the endpoint refuses to invent.
    /// </summary>
    /// <param name="req">The incoming request.</param>
    /// <param name="derivedType">The conversation side derived from how the caller authenticated.</param>
    /// <returns>Success, or the failure describing the first missing or unacceptable field.</returns>
    /// <remarks>
    /// Separate from <c>HandleAsync</c> because the three checks are what pushed that method past
    /// the FDW007 complexity threshold, and because a condition that needs a result should return
    /// one rather than write an error into endpoint state from three places.
    /// </remarks>
    private IGenericResult Validate(SendMessageRequest req, string derivedType)
    {
        if (string.IsNullOrWhiteSpace(req.ReferenceId))
        {
            return GenericResult.Failure(MessagingEndpointLog.ReferenceIdMissing(_logger));
        }

        // A caller may state its side, but only to be checked against the derived one. Contradiction
        // is refused rather than corrected, because a client that believes it is the other party has
        // a bug worth surfacing, not a value worth silently overwriting.
        if (req.MessageType is not null
            && !string.Equals(req.MessageType, derivedType, StringComparison.Ordinal))
        {
            return GenericResult.Failure(
                MessagingEndpointLog.MessageTypeRefused(_logger, req.MessageType));
        }

        if (string.IsNullOrWhiteSpace(req.Subject))
        {
            return GenericResult.Failure(
                MessagingEndpointLog.SubjectMissing(_logger, req.ReferenceId));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(SendMessageRequest req, CancellationToken ct)
    {
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

        // Direction is DERIVED, never taken from the body. A caller that could name its own side
        // could post as the other one, and a transcript that lets the sender choose how it is
        // attributed is a transcript that can lie about who said something.
        var derivedType = string.Equals(
            HttpContext.User.Identity?.AuthenticationType,
            AuthenticationSchemes.PatBearer,
            StringComparison.Ordinal)
                ? AgentMessageType
                : UserReplyType;

        var validation = Validate(req, derivedType);
        if (validation.IsFailure)
        {
            AddError(validation.CurrentMessage!);
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        // Why a local: Validate has already proved both are present, but that proof does not cross
        // the method boundary for the compiler. The value is read four times below.
        var referenceId = req.ReferenceId!;

        MessagingEndpointLog.SendingMessage(_logger, referenceId);

        try
        {
            var result = await _messageService.CreateMessage(
                new CreateMessageRequest
                {
                    TenantId = tenantId,
                    RecipientUserId = req.RecipientUserId,
                    SenderUserId = userId,
                    MessageType = derivedType,
                    Subject = req.Subject!,
                    Body = req.Body,
                    ReferenceId = referenceId
                },
                ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                MessagingEndpointLog.MessageSendFailed(_logger, referenceId, result.CurrentMessage!);
                AddError("Failed to send message");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            MessagingEndpointLog.MessageSent(_logger, result.Value!.Id.ToString(), referenceId);
            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MessagingEndpointLog.MessagingException(_logger, ex, "send-message");
            await Send.StatusCodeAsync(500, ct).ConfigureAwait(false);
        }
    }
}
