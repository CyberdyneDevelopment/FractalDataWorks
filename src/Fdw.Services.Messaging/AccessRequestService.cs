using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default implementation of <see cref="IAccessRequestService"/> using <see cref="IDataGateway"/>
/// for database-backed access request persistence.
/// </summary>
public sealed class AccessRequestService : IAccessRequestService
{
    private const string DataStoreName = "OpsDb";
    private const string PathName = "msg";
    private const string MessageContainer = "Message";
    private const string AccessRequestContainer = "AccessRequest";

    private readonly ILogger<AccessRequestService> _logger;
    private readonly IDataGateway _dataGateway;
    private readonly IMessageService _messageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessRequestService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateway">The data gateway for access request data access.</param>
    /// <param name="messageService">The message service for creating associated messages.</param>
    public AccessRequestService(
        ILogger<AccessRequestService> logger,
        IDataGateway dataGateway,
        IMessageService messageService)
    {
        _logger = logger ?? NullLogger<AccessRequestService>.Instance;
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AccessRequestPayload>> RequestAccess(
        CreateAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceRequestAccessEntry(_logger);

        try
        {
            var messageRequest = new CreateMessageRequest
            {
                TenantId = request.TenantId,
                SenderUserId = request.RequestingUserId,
                MessageType = "AccessRequest",
                Severity = "Info",
                Subject = $"Access request: {request.RequestedPermission} on {request.RequestedResource}",
                Body = request.Justification,
                ResourceType = "AccessRequest",
                ResourceId = request.RequestedResource,
                ReferenceId = request.ReferenceId
            };

            var messageResult = await _messageService.CreateMessage(messageRequest, cancellationToken)
                .ConfigureAwait(false);

            if (!messageResult.IsSuccess)
            {
                return messageResult.Messages.Any()
                    ? messageResult.ToNewResult<AccessRequestPayload>()
                    : GenericResult<AccessRequestPayload>.Failure(
                        MessagingLog.AccessRequestFailed(_logger, "new", "Failed to create associated message"));
            }

            var accessRequestId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var insertRecord = new AccessRequestInsertRecord
            {
                Id = accessRequestId,
                MessageId = messageResult.Value!.Id,
                RequestedResource = request.RequestedResource,
                RequestedPermission = request.RequestedPermission,
                Justification = request.Justification,
                Status = "Pending",
                CreatedAt = now
            };

            var command = CmdBuilders.Insert.Into<AccessRequestInsertRecord>(AccessRequestContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Value(insertRecord);

            var insertResult = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!insertResult.IsSuccess)
            {
                return insertResult.Messages.Any()
                    ? insertResult.ToNewResult<AccessRequestPayload>()
                    : GenericResult<AccessRequestPayload>.Failure(
                        MessagingLog.AccessRequestFailed(_logger, "new", "Insert command failed"));
            }

            MessagingLog.AccessRequestCreated(
                _logger,
                request.RequestedResource,
                request.RequestingUserId.ToString("D"));

            var dto = new AccessRequestPayload
            {
                Id = accessRequestId,
                MessageId = messageResult.Value!.Id,
                RequestedResource = request.RequestedResource,
                RequestedPermission = request.RequestedPermission,
                Justification = request.Justification,
                Status = "Pending",
                CreatedAt = now
            };

            return GenericResult<AccessRequestPayload>.Success(dto);
        }
        catch (Exception ex)
        {
            return GenericResult<AccessRequestPayload>.Failure(
                MessagingLog.AccessRequestFailed(_logger, "new", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Approve(
        Guid requestId,
        Guid reviewerUserId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceApproveEntry(_logger);

        try
        {
            var requestIdStr = requestId.ToString("D");
            var reviewerIdStr = reviewerUserId.ToString("D");
            var now = DateTime.UtcNow;

            var command = CmdBuilders.Update.In<AccessRequestReviewUpdate>(AccessRequestContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(AccessRequestPayload.Id), requestId)
                .Value(new AccessRequestReviewUpdate
                {
                    Status = "Approved",
                    ReviewedByUserId = reviewerUserId,
                    ReviewedAt = now,
                    ReviewNotes = notes,
                    ModifiedAt = now
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.AccessRequestFailed(_logger, requestIdStr, "Update command failed"));
            }

            if (result.Value == 0)
            {
                return GenericResult.Failure(
                    MessagingLog.AccessRequestFailed(_logger, requestIdStr, "AccessRequest not found"));
            }

            MessagingLog.AccessRequestApproved(_logger, requestIdStr, reviewerIdStr);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MessagingLog.AccessRequestFailed(_logger, requestId.ToString("D"), ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Deny(
        Guid requestId,
        Guid reviewerUserId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceDenyEntry(_logger);

        try
        {
            var requestIdStr = requestId.ToString("D");
            var reviewerIdStr = reviewerUserId.ToString("D");
            var now = DateTime.UtcNow;

            var command = CmdBuilders.Update.In<AccessRequestReviewUpdate>(AccessRequestContainer)
                .DataStore(DataStoreName).Path(PathName)
                .Where(nameof(AccessRequestPayload.Id), requestId)
                .Value(new AccessRequestReviewUpdate
                {
                    Status = "Denied",
                    ReviewedByUserId = reviewerUserId,
                    ReviewedAt = now,
                    ReviewNotes = notes,
                    ModifiedAt = now
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? (IGenericResult)result
                    : GenericResult.Failure(
                        MessagingLog.AccessRequestFailed(_logger, requestIdStr, "Update command failed"));
            }

            if (result.Value == 0)
            {
                return GenericResult.Failure(
                    MessagingLog.AccessRequestFailed(_logger, requestIdStr, "AccessRequest not found"));
            }

            MessagingLog.AccessRequestDenied(_logger, requestIdStr, reviewerIdStr);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                MessagingLog.AccessRequestFailed(_logger, requestId.ToString("D"), ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AccessRequestPayload>>> GetPending(
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetPendingEntry(_logger);

        try
        {
            var builder = Query.From<AccessRequestPayload>(DataStoreName, PathName, AccessRequestContainer)
                .Where(r => r.Status).Equal("Pending");

            var command = builder.Build();

            var result = await _dataGateway.Execute<IEnumerable<AccessRequestPayload>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.Messages.Any()
                    ? result.ToNewResult<IReadOnlyList<AccessRequestPayload>>()
                    : GenericResult<IReadOnlyList<AccessRequestPayload>>.Failure(
                        MessagingLog.AccessRequestFailed(_logger, "pending-query", "Query command failed"));
            }

            var requests = result.Value?.ToList();
            if (requests is null)
            {
                return GenericResult<IReadOnlyList<AccessRequestPayload>>.Failure(
                    MessagingLog.AccessRequestFailed(_logger, "query", "Query returned null value"));
            }

            return GenericResult<IReadOnlyList<AccessRequestPayload>>.Success(requests);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<AccessRequestPayload>>.Failure(
                MessagingLog.AccessRequestFailed(_logger, "pending-query", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AccessRequestPayload>>> GetForUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceGetForUserEntry(_logger);

        try
        {
            var messageCommand = Query.From<MessagePayload>(DataStoreName, PathName, MessageContainer)
                .Where(m => m.SenderUserId).Equal(userId)
                .Where(m => m.MessageType).Equal("AccessRequest")
                .Build();

            var messageResult = await _dataGateway.Execute<IEnumerable<MessagePayload>>(messageCommand, cancellationToken)
                .ConfigureAwait(false);

            if (!messageResult.IsSuccess)
            {
                return messageResult.ToNewResult<IReadOnlyList<AccessRequestPayload>>();
            }

            var messageIds = messageResult.Value?.Select(m => m.Id).ToList();
            if (messageIds is null || messageIds.Count == 0)
            {
                return GenericResult<IReadOnlyList<AccessRequestPayload>>.Success(
                    Array.Empty<AccessRequestPayload>());
            }

            var requests = new List<AccessRequestPayload>();
            foreach (var messageId in messageIds)
            {
                var arCommand = Query.From<AccessRequestPayload>(DataStoreName, PathName, AccessRequestContainer)
                    .Where(r => r.MessageId).Equal(messageId)
                    .Build();

                var arResult = await _dataGateway.Execute<IEnumerable<AccessRequestPayload>>(arCommand, cancellationToken)
                    .ConfigureAwait(false);

                if (!arResult.IsSuccess)
                {
                    return arResult.ToNewResult<IReadOnlyList<AccessRequestPayload>>();
                }

                if (arResult.Value is not null)
                {
                    requests.AddRange(arResult.Value);
                }
            }

            return GenericResult<IReadOnlyList<AccessRequestPayload>>.Success(requests);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<AccessRequestPayload>>.Failure(
                MessagingLog.AccessRequestFailed(_logger, userId.ToString("D"), ex.Message));
        }
    }

    /// <summary>
    /// Internal record for inserting into msg.AccessRequest table.
    /// </summary>
    private sealed class AccessRequestInsertRecord
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string RequestedResource { get; set; } = string.Empty;
        public string RequestedPermission { get; set; } = string.Empty;
        public string? Justification { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Internal record for updating access request review fields.
    /// </summary>
    private sealed class AccessRequestReviewUpdate
    {
        public string? Status { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
