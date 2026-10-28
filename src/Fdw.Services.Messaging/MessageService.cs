using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Hubs;
using Fdw.Services.Messaging.Logging;
using Fdw.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default implementation of <see cref="IMessageService"/> using <see cref="IDataGateway"/>
/// for database-backed message persistence and SignalR for real-time delivery.
/// </summary>
/// <remarks>
/// It is also the source of the <c>Message</c> kind of alert (FDW-2429): a message addressed to a person is an alert they can acknowledge, snooze, silence,
/// dismiss or discard. What a person did to a message is kept by the alert action service, and a message read carries the time the caller dismissed it.
/// </remarks>
public sealed class MessageService : IMessageService, IAlertSource
{
    // The row's own name, not a store choice: this deployment has exactly one configured
    // messaging service, and every method resolves its store/path from that row rather than
    // from a value baked into this class.
    private const string MessagingServiceName = "Messaging";
    private const string MessageContainer = "Message";
    private const string RecipientContainer = "MessageRecipient";
    private const string MessagesReadPermission = "messages:read";

    private readonly ILogger<MessageService> _logger;
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");
    private readonly IMessagingConfigurationProvider _messaging;
    private readonly IHubContext<MessageHub, IMessageHubClient> _hubContext;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly AlertStore _alerts;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateways">The data gateway for message data access.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messages.</param>
    /// <param name="hubContext">The strongly-typed SignalR hub context for real-time message push.</param>
    /// <param name="authContext">Supplies the caller, who is the person a message must be addressed to for its alert to be theirs.</param>
    public MessageService(
        ILogger<MessageService> logger,
        IDataGatewayProvider dataGateways,
        IMessagingConfigurationProvider messaging,
        IHubContext<MessageHub, IMessageHubClient> hubContext,
        IAuthenticationContextAccessor authContext)
    {
        _logger = logger ?? NullLogger<MessageService>.Instance;
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _messaging = messaging ?? throw new ArgumentNullException(nameof(messaging));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _alerts = new AlertStore(_logger, dataGateways, messaging);
    }

    /// <summary>Resolves the store and path this deployment keeps its messages in.</summary>
    /// <remarks>
    /// Read fresh on every call rather than cached at construction: the row is ordinary
    /// configuration, and a change to it should take effect on the next request rather than
    /// require a restart. <see cref="MessagingConfigurationProvider"/> caches the underlying read
    /// at the gateway layer, so this is not a query per call.
    /// </remarks>
    private async Task<IGenericResult<(string DataStoreName, string PathName)>> ResolveLocation(
        CancellationToken cancellationToken)
    {
        var messaging = await _messaging.Get(MessagingServiceName, cancellationToken).ConfigureAwait(false);
        if (!messaging.IsSuccess)
            return messaging.ToNewResult<(string, string)>();

        // Success with no value is its own state, not a failure to convert. Folding it into the
        // branch above called ToNewResult on a SUCCESSFUL result, which throws "Cannot convert a
        // successful result without providing a value" -- so a missing Messaging row surfaced as a
        // complaint about result plumbing and said nothing about the row.
        if (messaging.Value is null)
            return GenericResult<(string, string)>.Failure(
                MessagingLog.LocationNotConfigured(
                    _logger, $"no Messaging row named '{MessagingServiceName}' exists"));

        if (string.IsNullOrWhiteSpace(messaging.Value.DataStoreName) || string.IsNullOrWhiteSpace(messaging.Value.PathName))
        {
            return GenericResult<(string, string)>.Failure(
                MessagingLog.LocationNotConfigured(_logger, "DataStoreName or PathName is unset on the Messaging row"));
        }

        return GenericResult<(string, string)>.Success((messaging.Value.DataStoreName, messaging.Value.PathName));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<MessagePayload>> CreateMessage(
        CreateMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceCreateMessageEntry(_logger);

        try
        {
            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return location.ToNewResult<MessagePayload>();

            var messageId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var command = CmdBuilders.Insert.Into<MessageInsertRecord>(MessageContainer)
                .DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Value(BuildInsertRecord(request, messageId, now));

            var insertResult = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!insertResult.IsSuccess)
            {
                return insertResult.Messages.Any()
                    ? insertResult.ToNewResult<MessagePayload>()
                    : GenericResult<MessagePayload>.Failure(
                        MessagingLog.MessageCreationFailed(_logger, "Insert command failed"));
            }

            var payload = BuildMessageDto(request, messageId, now);

            await InsertRecipientIfDirect(request, messageId, now, location.Value, cancellationToken).ConfigureAwait(false);
            await NotifyRecipientViaSignalR(payload).ConfigureAwait(false);

            var recipientId = request.RecipientUserId?.ToString("D") ?? "broadcast";
            MessagingLog.MessageCreated(_logger, request.Subject, recipientId);

            return GenericResult<MessagePayload>.Success(payload);
        }
        catch (Exception ex)
        {
            return GenericResult<MessagePayload>.Failure(
                MessagingLog.MessageCreationFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<MessagePayload>>> GetMessages(
        MessageQuery query,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetMessagesEntry(_logger);

        if (WindowRefusal(query) is { } refusal)
        {
            return GenericResult<IReadOnlyList<MessagePayload>>.Failure(refusal);
        }

        try
        {
            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return location.ToNewResult<IReadOnlyList<MessagePayload>>();

            var builder = ApplyFilters(
                Query.From<MessagePayload>(location.Value.DataStoreName, location.Value.PathName, MessageContainer),
                query);

            // Why the cursor row is read first: a keyset window is a predicate over the SORT KEY,
            // not over the id — "everything after (CreatedAt, Id)". The caller names a message, so
            // its own CreatedAt has to be in hand before the predicate can be written at all.
            // An absent cursor fails here rather than degrading to an unwindowed read.
            var cursorId = query.After ?? query.Before;
            MessagePayload? cursor = null;

            if (cursorId.HasValue)
            {
                var cursorResult = await GetMessage(cursorId.Value, cancellationToken).ConfigureAwait(false);
                if (cursorResult.IsFailure)
                {
                    return GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                        MessagingLog.PagingCursorNotFound(_logger, cursorId.Value.ToString("D")));
                }

                cursor = cursorResult.Value;
            }

            var result = await Gateway
                .Execute<IEnumerable<MessagePayload>>(BuildWindow(builder, query, cursor), cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? result.ToNewResult<IReadOnlyList<MessagePayload>>()
                    : GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                        MessagingLog.MessageQueryFailed(_logger, "Query command failed"));
            }

            if (result.Value is null)
            {
                return GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                    MessagingLog.MessageQueryFailed(_logger, "Query returned null value"));
            }

            // Scrollback is read backwards from the cursor so the store returns the LAST page rather
            // than the first, then flipped so callers always receive one chronological order. The
            // reversal spans at most Take rows, not the set.
            var items = query.Before.HasValue
                ? (IReadOnlyList<MessagePayload>)[.. result.Value.Reverse()]
                : [.. result.Value];

            var dismissed = await ApplyDismissals(items, cancellationToken).ConfigureAwait(false);
            if (!dismissed.IsSuccess)
                return dismissed.ToNewResult<IReadOnlyList<MessagePayload>>();

            var userId = query.UserId.ToString("D");
            MessagingLog.MessagesQueried(_logger, userId, items.Count);

            return GenericResult<IReadOnlyList<MessagePayload>>.Success(items);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    // Why a query's window cannot be read, or null when it can: both cursors name a direction, and an order would name a second one.
    private IGenericMessage? WindowRefusal(MessageQuery query)
    {
        if (query.After.HasValue && query.Before.HasValue)
            return MessagingLog.PagingCursorsConflict(_logger);

        return query.NewestFirst && (query.After.HasValue || query.Before.HasValue)
            ? MessagingLog.PagingOrderWithCursor(_logger)
            : null;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<int>> CountMessages(
        MessageQuery query,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetMessagesEntry(_logger);

        try
        {
            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return location.ToNewResult<int>();

            var command = ApplyFilters(
                Query.From<MessagePayload>(location.Value.DataStoreName, location.Value.PathName, MessageContainer),
                query).Build();

            var result = await Gateway.Execute<IEnumerable<MessagePayload>>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? result.ToNewResult<int>()
                    : GenericResult<int>.Failure(MessagingLog.MessageQueryFailed(_logger, "Message count query failed"));
            }

            return result.Value is null
                ? GenericResult<int>.Failure(MessagingLog.MessageQueryFailed(_logger, "Message count query returned null value"))
                : GenericResult<int>.Success(result.Value.Count());
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Failure(
                MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<MessagePayload>> GetMessage(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetMessageEntry(_logger);

        try
        {
            var found = await FindMessage(messageId, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
                return found.ToNewResult<MessagePayload>();

            if (found.Value is not { } message)
            {
                return GenericResult<MessagePayload>.Failure(
                    MessagingLog.MessageNotFound(_logger, messageId.ToString("D")));
            }

            var dismissed = await ApplyDismissals([message], cancellationToken).ConfigureAwait(false);
            if (!dismissed.IsSuccess)
                return dismissed.ToNewResult<MessagePayload>();

            return GenericResult<MessagePayload>.Success(message);
        }
        catch (Exception ex)
        {
            return GenericResult<MessagePayload>.Failure(
                MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    // One message by its identifier, or success with no value when there is none: the question GetMessage answers with a failure and Find with "not found".
    private async Task<IGenericResult<MessagePayload?>> FindMessage(Guid messageId, CancellationToken cancellationToken)
    {
        var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<MessagePayload?>();

        var command = Query.From<MessagePayload>(location.Value.DataStoreName, location.Value.PathName, MessageContainer)
            .Where(m => m.Id).Equal(messageId)
            .Build();

        var result = await Gateway.Execute<IEnumerable<MessagePayload>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.Messages.Any()
                ? result.ToNewResult<MessagePayload?>()
                : GenericResult<MessagePayload?>.Failure(
                    MessagingLog.MessageQueryFailed(_logger, "Query command failed"));
        }

        return GenericResult<MessagePayload?>.Success(result.Value?.FirstOrDefault());
    }

    // The time each person dismissed their messages is the Dismiss action of the message's alert, not a column of the message: the page is given the time of
    // the Dismiss row of its recipient, and none for a message that person has not dismissed.
    private async Task<IGenericResult> ApplyDismissals(IReadOnlyList<MessagePayload> messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
            message.DismissedAt = null;

        foreach (var person in messages.Where(m => m.RecipientUserId.HasValue).GroupBy(m => m.RecipientUserId!.Value))
        {
            var actions = await _alerts.FindActions(MessageAlert.Kind, [.. person.Select(m => m.Id)], person.Key, cancellationToken).ConfigureAwait(false);
            if (!actions.IsSuccess)
                return (IGenericResult)actions;

            foreach (var action in actions.Value!.Where(a => string.Equals(a.Action, MessageAlert.DismissAction, StringComparison.OrdinalIgnoreCase)))
            {
                var dismissed = person.FirstOrDefault(m => m.Id == action.AlertId);
                if (dismissed is not null)
                    dismissed.DismissedAt = action.ActedAt.UtcDateTime;
            }
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<int>> GetUnreadCount(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetUnreadCountEntry(_logger);

        try
        {
            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return location.ToNewResult<int>();

            var command = Query.From<MessagePayload>(location.Value.DataStoreName, location.Value.PathName, MessageContainer)
                .Where(m => m.RecipientUserId).Equal(userId)
                .Where(m => m.ReadAt).IsNull()
                .Build();

            var result = await Gateway.Execute<IEnumerable<MessagePayload>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? result.ToNewResult<int>()
                    : GenericResult<int>.Failure(
                        MessagingLog.MessageQueryFailed(_logger, "Unread count query failed"));
            }

            var count = result.Value?.Count() ?? 0;
            return GenericResult<int>.Success(count);
        }
        catch (Exception ex)
        {
            return GenericResult<int>.Failure(
                MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MarkDelivered(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceMarkDeliveredEntry(_logger);

        try
        {
            var messageIdStr = messageId.ToString("D");

            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return (IGenericResult)location;

            var command = CmdBuilders.Update.In<MessageDeliveredUpdate>(MessageContainer)
                .DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageDeliveredUpdate
                {
                    Status = "Delivered",
                    DeliveredAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.MessageUpdateFailed(_logger, messageIdStr, "Update command failed"));
            }

            MessagingLog.MessageDelivered(_logger, messageIdStr);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MessagingLog.MessageUpdateFailed(_logger, messageId.ToString("D"), ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MarkRead(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceMarkReadEntry(_logger);

        try
        {
            var messageIdStr = messageId.ToString("D");

            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return (IGenericResult)location;

            var command = CmdBuilders.Update.In<MessageReadUpdate>(MessageContainer)
                .DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageReadUpdate
                {
                    Status = "Read",
                    ReadAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.MessageUpdateFailed(_logger, messageIdStr, "Update command failed"));
            }

            MessagingLog.MessageRead(_logger, messageIdStr);

            await NotifyMessageReadViaSignalR(messageId, cancellationToken).ConfigureAwait(false);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MessagingLog.MessageUpdateFailed(_logger, messageId.ToString("D"), ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Archive(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceArchiveEntry(_logger);

        try
        {
            var messageIdStr = messageId.ToString("D");

            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return (IGenericResult)location;

            var command = CmdBuilders.Update.In<MessageArchivedUpdate>(MessageContainer)
                .DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageArchivedUpdate
                {
                    Status = "Archived",
                    ArchivedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.MessageUpdateFailed(_logger, messageIdStr, "Update command failed"));
            }

            if (result.Value == 0)
            {
                return GenericResult.Failure(MessagingLog.MessageNotFound(_logger, messageIdStr));
            }

            MessagingLog.MessageArchived(_logger, messageIdStr);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MessagingLog.MessageUpdateFailed(_logger, messageId.ToString("D"), ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> MarkAllRead(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceMarkAllReadEntry(_logger);

        try
        {
            var userIdStr = userId.ToString("D");
            var now = DateTime.UtcNow;

            var location = await ResolveLocation(cancellationToken).ConfigureAwait(false);
            if (!location.IsSuccess)
                return (IGenericResult)location;

            var command = CmdBuilders.Update.In<MessageReadUpdate>(MessageContainer)
                .DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(MessagePayload.RecipientUserId), userId)
                .Where(nameof(MessagePayload.ReadAt), new IsNullOperator(), null)
                .Value(new MessageReadUpdate
                {
                    Status = "Read",
                    ReadAt = now,
                    ModifiedAt = now
                });

            var result = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.MessageUpdateFailed(_logger, userIdStr, "Bulk update command failed"));
            }

            MessagingLog.AllMessagesRead(_logger, userIdStr);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc />
    public string Kind => MessageAlert.Kind;

    /// <inheritdoc />
    // Why every action for every message: the reference default; a company's own choice of the set is read elsewhere (FDW-2430).
    public IReadOnlyList<string> OfferedActions { get; } = ["Acknowledge", "Snooze", "Silence", "Dismiss", "Discard"];

    /// <inheritdoc />
    // Why only the recipient's: an alert is shown to one person, so a message addressed to someone else, or to nobody, does not exist for the caller.
    public async Task<IGenericResult<AlertSubject?>> Find(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Find an alert of a message");

        try
        {
            if (_authContext.Current is not { IsAuthenticated: true } context || !Guid.TryParse(context.UserId, out var userId))
                return GenericResult<AlertSubject?>.Failure(MessagingLog.AlertCallerRequired(_logger, "find an alert of a message"));

            // Why a right: the alert routes ask only for a signed-in person, and the message routes need messages:read.
            if (!PermissionCoverage.Covers(context.Permissions, MessagesReadPermission))
                return GenericResult<AlertSubject?>.Success(null);

            var found = await FindMessage(id, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
                return found.ToNewResult<AlertSubject?>();

            return GenericResult<AlertSubject?>.Success(
                found.Value is { } message && message.RecipientUserId == userId ? new AlertSubject(message.Id, message.TenantId) : null);
        }
        catch (Exception ex)
        {
            return GenericResult<AlertSubject?>.Failure(MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc />
    // Why read after a personal action: a person who acknowledges, snoozes, silences or dismisses a message has dealt with it, so it is marked read; a message
    // already read keeps the time it was read. An action taken for everyone is not the recipient's own and leaves the message as it is.
    public async Task<IGenericResult> Acted(Guid id, IAlertAction action, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Act on an alert of a message");

        try
        {
            if (action.ForEveryone)
                return GenericResult.Success();

            var found = await FindMessage(id, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
                return (IGenericResult)found;

            if (found.Value is not { } message)
                return GenericResult.Failure(MessagingLog.MessageNotFound(_logger, id.ToString("D")));

            return message.ReadAt is null
                ? await MarkRead(id, cancellationToken).ConfigureAwait(false)
                : GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(MessagingLog.MessageUpdateFailed(_logger, id.ToString("D"), ex.Message));
        }
    }

    private static MessageInsertRecord BuildInsertRecord(CreateMessageRequest request, Guid messageId, DateTime now)
    {
        return new MessageInsertRecord
        {
            Id = messageId,
            TenantId = request.TenantId,
            RecipientUserId = request.RecipientUserId,
            SenderUserId = request.SenderUserId,
            MessageType = request.MessageType,
            Severity = request.Severity,
            Subject = request.Subject,
            Body = request.Body,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            ReferenceId = request.ReferenceId,
            ActionType = request.ActionType,
            ActionUrl = request.ActionUrl,
            Status = "New",
            CreatedAt = now
        };
    }

    private static MessagePayload BuildMessageDto(CreateMessageRequest request, Guid messageId, DateTime now)
    {
        return new MessagePayload
        {
            Id = messageId,
            TenantId = request.TenantId,
            RecipientUserId = request.RecipientUserId,
            SenderUserId = request.SenderUserId,
            MessageType = request.MessageType,
            Severity = request.Severity,
            Subject = request.Subject,
            Body = request.Body,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            ReferenceId = request.ReferenceId,
            ActionType = request.ActionType,
            ActionUrl = request.ActionUrl,
            Status = "New",
            CreatedAt = now
        };
    }

    private async Task InsertRecipientIfDirect(
        CreateMessageRequest request,
        Guid messageId,
        DateTime now,
        (string DataStoreName, string PathName) location,
        CancellationToken cancellationToken)
    {
        if (!request.RecipientUserId.HasValue)
        {
            return;
        }

        var recipientRecord = new MessageRecipientInsertRecord
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            UserId = request.RecipientUserId.Value,
            RecipientType = "Direct",
            Status = "New",
            CreatedAt = now
        };

        var recipientCommand = CmdBuilders.Insert.Into<MessageRecipientInsertRecord>(RecipientContainer)
            .DataStore(location.DataStoreName).Path(location.PathName)
            .Value(recipientRecord);

        var recipientResult = await Gateway.Execute<int>(recipientCommand, cancellationToken).ConfigureAwait(false);
        if (!recipientResult.IsSuccess)
        {
            MessagingLog.MessageUpdateFailed(
                _logger,
                messageId.ToString("D"),
                "Failed to insert message recipient record");
        }
    }

    /// <summary>
    /// Narrows a message query to the rows the caller asked for.
    /// </summary>
    /// <param name="builder">A query over the message container.</param>
    /// <param name="query">The query carrying the filters.</param>
    /// <returns>The builder with every supplied filter applied.</returns>
    /// <remarks>
    /// The recipient is always constrained; every other filter is optional and omitted when unset,
    /// so an absent filter widens the result rather than matching empty. Separate from the query
    /// method because the two together cross the FDW007 complexity threshold, and because "which
    /// rows" and "which window over them" are different jobs.
    /// </remarks>
    private static QueryCommandBuilder<MessagePayload> ApplyFilters(
        QueryCommandBuilder<MessagePayload> builder,
        MessageQuery query)
    {
        builder = builder.Where(m => m.RecipientUserId).Equal(query.UserId);

        if (query.TenantId.HasValue)
        {
            builder = builder.Where(m => m.TenantId).Equal(query.TenantId.Value);
        }

        if (!string.IsNullOrEmpty(query.MessageType))
        {
            builder = builder.Where(m => m.MessageType).Equal(query.MessageType);
        }

        if (!string.IsNullOrEmpty(query.Severity))
        {
            builder = builder.Where(m => m.Severity).Equal(query.Severity);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            builder = builder.Where(m => m.Status).Equal(query.Status);
        }

        if (!string.IsNullOrEmpty(query.ReferenceId))
        {
            builder = builder.Where(m => m.ReferenceId).Equal(query.ReferenceId);
        }

        return builder;
    }

    /// <summary>
    /// Puts the ordering and the requested window onto the command.
    /// </summary>
    /// <param name="builder">The query with its filters already applied.</param>
    /// <param name="query">The query carrying the window.</param>
    /// <param name="cursor">The resolved cursor row, or <see langword="null"/> for offset paging.</param>
    /// <returns>The built command.</returns>
    /// <remarks>
    /// Ordering is (CreatedAt, Id): a burst written in one transaction shares a timestamp, and a tie
    /// with no second key has no defined order. Id is Guid.NewGuid() — random — so the tiebreak is
    /// stable but not insertion order; minting with Guid.CreateVersion7() would make it both.
    ///
    /// The predicate and the ORDER BY are evaluated by the SAME store, which is what makes the guid
    /// leg sound: SQL Server compares uniqueidentifier in its own byte order, not .NET's, so a
    /// window computed here and an order computed there would disagree about ties. Both sides being
    /// server-side, they cannot.
    ///
    /// Separate from the query method so neither crosses the FDW007 complexity threshold.
    /// </remarks>
    private static DataGatewayCall BuildWindow(
        QueryCommandBuilder<MessagePayload> builder,
        MessageQuery query,
        MessagePayload? cursor)
    {
        if (cursor is null)
        {
            return (query.NewestFirst
                    ? builder.OrderByDescending(m => m.CreatedAt).OrderByDescending(m => m.Id)
                    : builder.OrderBy(m => m.CreatedAt).OrderBy(m => m.Id))
                .Paging(query.Skip, query.Take)
                .Build();
        }

        // "Strictly past the cursor" in sort-key terms: a later timestamp, or the same timestamp and
        // a later id. Written as a group so it ANDs with the filters rather than replacing them.
        var forward = query.After.HasValue;

        builder = builder.BeginOrGroup();
        builder = forward
            ? builder.Where(m => m.CreatedAt).GreaterThan(cursor.CreatedAt)
            : builder.Where(m => m.CreatedAt).LessThan(cursor.CreatedAt);

        builder = builder.BeginAndGroup().Where(m => m.CreatedAt).Equal(cursor.CreatedAt);
        builder = forward
            ? builder.Where(m => m.Id).GreaterThan(cursor.Id)
            : builder.Where(m => m.Id).LessThan(cursor.Id);
        builder = builder.EndGroup().EndGroup();

        builder = forward
            ? builder.OrderBy(m => m.CreatedAt).OrderBy(m => m.Id)
            : builder.OrderByDescending(m => m.CreatedAt).OrderByDescending(m => m.Id);

        return builder.Paging(0, query.Take).Build();
    }

    private async Task NotifyRecipientViaSignalR(MessagePayload payload)
    {
        if (!payload.RecipientUserId.HasValue)
        {
            return;
        }

        var recipientId = payload.RecipientUserId.Value.ToString("D");
        await _hubContext.Clients.Group(HubScopes.User(recipientId)).NewMessage(payload).ConfigureAwait(false);
        await _hubContext.Clients.Group(HubScopes.User(recipientId)).UnreadCountChanged().ConfigureAwait(false);
    }

    private async Task NotifyMessageReadViaSignalR(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var messageResult = await GetMessage(messageId, cancellationToken).ConfigureAwait(false);
        if (!messageResult.IsSuccess || messageResult.Value?.RecipientUserId is not { } recipientUserId)
        {
            return;
        }

        var recipientId = recipientUserId.ToString("D");
        await _hubContext.Clients.Group(HubScopes.User(recipientId)).MessageRead(messageId).ConfigureAwait(false);
        await _hubContext.Clients.Group(HubScopes.User(recipientId)).UnreadCountChanged().ConfigureAwait(false);
    }

    /// <summary>
    /// Internal record for inserting into msg.Message table.
    /// </summary>
    private sealed class MessageInsertRecord
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? RecipientUserId { get; set; }
        public Guid? SenderUserId { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string? ResourceType { get; set; }
        public string? ResourceId { get; set; }
        public string? ReferenceId { get; set; }
        public string? ActionType { get; set; }
        public string? ActionUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Internal record for inserting into msg.MessageRecipient table.
    /// </summary>
    private sealed class MessageRecipientInsertRecord
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public string RecipientType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }


    /// <summary>Update record for the Delivered transition (Status + DeliveredAt only).</summary>
    private sealed class MessageDeliveredUpdate
    {
        public string? Status { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>Update record for the Read transition (Status + ReadAt only).</summary>
    private sealed class MessageReadUpdate
    {
        public string? Status { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>Update record for the Archived transition (Status + ArchivedAt only).</summary>
    private sealed class MessageArchivedUpdate
    {
        public string? Status { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
