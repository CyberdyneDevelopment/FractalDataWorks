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
    // Why: msg.Message and msg.MessageRecipient live in OpsDb (operational data store),
    // not a separate MessagingDb. There is no MessagingDb DataStore — the msg schema is
    // one of OpsDb's paths (alongside ops, etl, sched, dq).
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

            await InsertRecipientIfDirect(request, messageId, now, cancellationToken).ConfigureAwait(false);
            await NotifyRecipientViaSignalR(request, messageId).ConfigureAwait(false);

            var recipientId = request.RecipientUserId?.ToString("D") ?? "broadcast";
            MessagingLog.MessageCreated(_logger, request.Subject, recipientId);

            return GenericResult<MessagePayload>.Success(BuildMessageDto(request, messageId, now));
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

        try
        {
            var builder = Query.From<MessagePayload>(DataStoreName, PathName, MessageContainer)
                .Where(m => m.RecipientUserId).Equal(query.UserId);

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

            var command = builder.Build();

            var result = await _dataGateway.Execute<IEnumerable<MessagePayload>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? result.ToNewResult<IReadOnlyList<MessagePayload>>()
                    : GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                        MessagingLog.MessageQueryFailed(_logger, "Query command failed"));
            }

            var items = result.Value?
                .Skip(query.Skip)
                .Take(query.Take)
                .ToList();

            if (items is null)
            {
                return GenericResult<IReadOnlyList<MessagePayload>>.Failure(
                    MessagingLog.MessageQueryFailed(_logger, "Query returned null value"));
            }

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
            // Why: unread = ReadAt IS NULL. Equal(null) emits "ReadAt = NULL" (never matches / translator
            // rejects null equality) — use IsNull() for correct IS NULL semantics. Same defect as MarkAllRead.
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

            // Why: "unread" is ReadAt IS NULL. The two-arg Where(name, null) builds an EqualOperator
            // (ReadAt = NULL), which never matches in SQL and the translator rejects a null equality
            // parameter — the source of the runtime 500. Use IsNullOperator for proper IS NULL semantics.
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

    // Why: SignalR typed-client interface methods do not take a CancellationToken (the FDW
    // CancellationToken-propagation rule explicitly exempts SignalR hub clients), so the push here
    // is fire-and-forget on the typed contract rather than a stringly-typed SendAsync.
    private async Task NotifyRecipientViaSignalR(
        CreateMessageRequest request,
        Guid messageId)
    {
        if (!request.RecipientUserId.HasValue)
        {
            return;
        }

        var recipientId = request.RecipientUserId.Value.ToString("D");
        await _hubContext.Clients.Group(recipientId).NewMessage(messageId).ConfigureAwait(false);
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

    // Why: One minimal update record per status transition. The MsSql UPDATE translator builds the
    // SET clause from EVERY property on the update object that maps to a container column. A single
    // shared record carrying all timestamps (Delivered/Read/Dismissed/Archived) would emit
    // "SET [DeliveredAt]=NULL, [DismissedAt]=NULL, [ArchivedAt]=NULL, ..." on every transition,
    // clobbering timestamps set by earlier transitions. Each record below sets ONLY the columns
    // that transition owns, so unrelated timestamps are left untouched.

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
