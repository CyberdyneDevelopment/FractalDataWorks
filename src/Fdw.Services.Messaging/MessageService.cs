using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Hubs;
using Fdw.Services.Messaging.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default implementation of <see cref="IMessageService"/> using <see cref="IDataGateway"/>
/// for database-backed message persistence and SignalR for real-time delivery.
/// </summary>
public sealed class MessageService : IMessageService
{
    private const string DataStoreName = "OpsDb";
    private const string PathName = "msg";
    private const string MessageContainer = "Message";
    private const string RecipientContainer = "MessageRecipient";

    private readonly ILogger<MessageService> _logger;
    private readonly IDataGateway _dataGateway;
    private readonly IHubContext<MessageHub, IMessageHubClient> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateway">The data gateway for message data access.</param>
    /// <param name="hubContext">The strongly-typed SignalR hub context for real-time message push.</param>
    public MessageService(
        ILogger<MessageService> logger,
        IDataGateway dataGateway,
        IHubContext<MessageHub, IMessageHubClient> hubContext)
    {
        _logger = logger ?? NullLogger<MessageService>.Instance;
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<MessagePayload>> CreateMessage(
        CreateMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceCreateMessageEntry(_logger);

        try
        {
            var messageId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var command = CmdBuilders.Insert.Into<MessageInsertRecord>(MessageContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Value(BuildInsertRecord(request, messageId, now));

            var insertResult = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!insertResult.IsSuccess)
            {
                return insertResult.Messages.Any()
                    ? insertResult.ToNewResult<MessagePayload>()
                    : GenericResult<MessagePayload>.Failure(
                        MessagingLog.MessageCreationFailed(_logger, "Insert command failed"));
            }

            var payload = BuildMessageDto(request, messageId, now);

            await InsertRecipientIfDirect(request, messageId, now, cancellationToken).ConfigureAwait(false);
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

        if (query.After.HasValue && query.Before.HasValue)
        {
            return GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                MessagingLog.PagingCursorsConflict(_logger));
        }

        try
        {
            var builder = ApplyFilters(
                Query.From<MessagePayload>(DataStoreName, PathName, MessageContainer),
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

            var result = await _dataGateway
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

    /// <inheritdoc />
    public async Task<IGenericResult<MessagePayload>> GetMessage(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetMessageEntry(_logger);

        try
        {
            var command = Query.From<MessagePayload>(DataStoreName, PathName, MessageContainer)
                .Where(m => m.Id).Equal(messageId)
                .Build();

            var result = await _dataGateway.Execute<IEnumerable<MessagePayload>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? result.ToNewResult<MessagePayload>()
                    : GenericResult<MessagePayload>.Failure(
                        MessagingLog.MessageQueryFailed(_logger, "Query command failed"));
            }

            var message = result.Value?.FirstOrDefault();
            if (message is null)
            {
                return GenericResult<MessagePayload>.Failure(
                    MessagingLog.MessageNotFound(_logger, messageId.ToString("D")));
            }

            return GenericResult<MessagePayload>.Success(message);
        }
        catch (Exception ex)
        {
            return GenericResult<MessagePayload>.Failure(
                MessagingLog.MessageQueryFailed(_logger, ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<int>> GetUnreadCount(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetUnreadCountEntry(_logger);

        try
        {
            var command = Query.From<MessagePayload>(DataStoreName, PathName, MessageContainer)
                .Where(m => m.RecipientUserId).Equal(userId)
                .Where(m => m.ReadAt).IsNull()
                .Build();

            var result = await _dataGateway.Execute<IEnumerable<MessagePayload>>(command, cancellationToken)
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

            var command = CmdBuilders.Update.In<MessageDeliveredUpdate>(MessageContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageDeliveredUpdate
                {
                    Status = "Delivered",
                    DeliveredAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

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

            var command = CmdBuilders.Update.In<MessageReadUpdate>(MessageContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageReadUpdate
                {
                    Status = "Read",
                    ReadAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

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
    public async Task<IGenericResult> Dismiss(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceDismissEntry(_logger);

        try
        {
            var messageIdStr = messageId.ToString("D");

            var command = CmdBuilders.Update.In<MessageDismissedUpdate>(MessageContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageDismissedUpdate
                {
                    Status = "Dismissed",
                    DismissedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.MessageUpdateFailed(_logger, messageIdStr, "Update command failed"));
            }

            MessagingLog.MessageDismissed(_logger, messageIdStr);
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

            var command = CmdBuilders.Update.In<MessageArchivedUpdate>(MessageContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(MessagePayload.Id), messageId)
                .Value(new MessageArchivedUpdate
                {
                    Status = "Archived",
                    ArchivedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.MessageUpdateFailed(_logger, messageIdStr, "Update command failed"));
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

            var command = CmdBuilders.Update.In<MessageReadUpdate>(MessageContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(MessagePayload.RecipientUserId), userId)
                .Where(nameof(MessagePayload.ReadAt), new IsNullOperator(), null)
                .Value(new MessageReadUpdate
                {
                    Status = "Read",
                    ReadAt = now,
                    ModifiedAt = now
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

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
            .DataStore(DataStoreName).Path(PathName)
            .Value(recipientRecord);

        var recipientResult = await _dataGateway.Execute<int>(recipientCommand, cancellationToken).ConfigureAwait(false);
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
            return builder
                .OrderBy(m => m.CreatedAt)
                .OrderBy(m => m.Id)
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
        await _hubContext.Clients.Group(recipientId).NewMessage(payload).ConfigureAwait(false);
        await _hubContext.Clients.Group(recipientId).UnreadCountChanged().ConfigureAwait(false);
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
        await _hubContext.Clients.Group(recipientId).MessageRead(messageId).ConfigureAwait(false);
        await _hubContext.Clients.Group(recipientId).UnreadCountChanged().ConfigureAwait(false);
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

    /// <summary>Update record for the Dismissed transition (Status + DismissedAt only).</summary>
    private sealed class MessageDismissedUpdate
    {
        public string? Status { get; set; }
        public DateTime? DismissedAt { get; set; }
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
