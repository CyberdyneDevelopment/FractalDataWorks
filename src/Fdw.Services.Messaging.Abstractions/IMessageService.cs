using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Service for managing in-system messages with lifecycle tracking.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Creates a new in-system message.
    /// </summary>
    /// <param name="request">The message creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created message.</returns>
    Task<IGenericResult<MessagePayload>> CreateMessage(CreateMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries messages matching the specified criteria.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of matching messages.</returns>
    Task<IGenericResult<IReadOnlyList<MessagePayload>>> GetMessages(MessageQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single message by its identifier.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message if found.</returns>
    Task<IGenericResult<MessagePayload>> GetMessage(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread messages for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unread message count.</returns>
    Task<IGenericResult<int>> GetUnreadCount(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as delivered.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> MarkDelivered(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as read.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> MarkRead(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a message.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> Dismiss(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a message.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> Archive(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all messages as read for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> MarkAllRead(Guid userId, CancellationToken cancellationToken = default);
}
