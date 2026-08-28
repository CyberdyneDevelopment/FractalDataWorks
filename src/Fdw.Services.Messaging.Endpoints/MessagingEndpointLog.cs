using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// MessageLogging for messaging endpoint operations.
/// EventId range: 7240-7279
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS7")]
public static partial class MessagingEndpointLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Message read operations (7240-7249)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when listing messages for a user.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "Listing messages for user '{userId}'")]
    public static partial IGenericMessage ListingMessages(ILogger logger, string userId);

    /// <summary>
    /// Logged at Information level when messages are listed.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Found {count} messages for user '{userId}'")]
    public static partial IGenericMessage MessagesListed(ILogger logger, int count, string userId);

    /// <summary>
    /// Logged at Error level when listing messages fails.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to list messages for user '{userId}': {reason}")]
    public static partial IGenericMessage MessageListFailed(ILogger logger, string userId, string reason);

    /// <summary>
    /// Logged at Trace level when fetching a message by ID.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "Fetching message '{messageId}'")]
    public static partial IGenericMessage FetchingMessage(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Information level when a message is retrieved.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Message '{messageId}' retrieved")]
    public static partial IGenericMessage MessageRetrieved(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Warning level when a message is not found.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning, Message = "Failed to fetch message '{messageId}': {reason}")]
    public static partial IGenericMessage MessageFetchFailed(ILogger logger, string messageId, string reason);

    /// <summary>
    /// Logged at Trace level when getting unread count.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Getting unread count for user '{userId}'")]
    public static partial IGenericMessage GettingUnreadCount(ILogger logger, string userId);

    /// <summary>
    /// Logged at Information level when unread count is retrieved.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Unread count for user '{userId}': {count}")]
    public static partial IGenericMessage UnreadCountRetrieved(ILogger logger, string userId, int count);

    /// <summary>
    /// Logged at Error level when getting unread count fails.
    /// </summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Failed to get unread count for user '{userId}': {reason}")]
    public static partial IGenericMessage UnreadCountFailed(ILogger logger, string userId, string reason);

    // ═══════════════════════════════════════════════════════════════════════════
    // Message state transitions (7250-7259)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when marking a message as read.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace, Message = "Marking message '{messageId}' as read")]
    public static partial IGenericMessage MarkingMessageRead(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Information level when a message is marked as read.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "Message '{messageId}' marked as read")]
    public static partial IGenericMessage MessageMarkedRead(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Error level when marking a message as read fails.
    /// </summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Failed to mark message '{messageId}' as read: {reason}")]
    public static partial IGenericMessage MarkReadFailed(ILogger logger, string messageId, string reason);

    /// <summary>
    /// Logged at Trace level when dismissing a message.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Dismissing message '{messageId}'")]
    public static partial IGenericMessage DismissingMessage(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Information level when a message is dismissed.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Message '{messageId}' dismissed")]
    public static partial IGenericMessage MessageDismissed(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Error level when dismissing a message fails.
    /// </summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "Failed to dismiss message '{messageId}': {reason}")]
    public static partial IGenericMessage DismissFailed(ILogger logger, string messageId, string reason);

    /// <summary>
    /// Logged at Trace level when archiving a message.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Archiving message '{messageId}'")]
    public static partial IGenericMessage ArchivingMessage(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Information level when a message is archived.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "Message '{messageId}' archived")]
    public static partial IGenericMessage MessageArchived(ILogger logger, string messageId);

    /// <summary>
    /// Logged at Error level when archiving a message fails.
    /// </summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error, Message = "Failed to archive message '{messageId}': {reason}")]
    public static partial IGenericMessage ArchiveFailed(ILogger logger, string messageId, string reason);

    /// <summary>
    /// Logged at Trace level when marking all messages as read.
    /// </summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "Marking all messages as read for user '{userId}'")]
    public static partial IGenericMessage MarkingAllRead(ILogger logger, string userId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Mark all read continued + Access requests (7260-7279)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Information level when all messages are marked as read.
    /// </summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "All messages marked as read for user '{userId}'")]
    public static partial IGenericMessage AllMessagesMarkedRead(ILogger logger, string userId);

    /// <summary>
    /// Logged at Error level when marking all messages as read fails.
    /// </summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error, Message = "Failed to mark all messages as read for user '{userId}': {reason}")]
    public static partial IGenericMessage MarkAllReadFailed(ILogger logger, string userId, string reason);

    /// <summary>
    /// Logged at Trace level when creating an access request.
    /// </summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace, Message = "Creating access request for resource '{resource}' permission '{permission}'")]
    public static partial IGenericMessage CreatingAccessRequest(ILogger logger, string resource, string permission);

    /// <summary>
    /// Logged at Information level when an access request is created.
    /// </summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information, Message = "Access request created with id '{requestId}'")]
    public static partial IGenericMessage AccessRequestCreated(ILogger logger, string requestId);

    /// <summary>
    /// Logged at Error level when creating an access request fails.
    /// </summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error, Message = "Failed to create access request: {reason}")]
    public static partial IGenericMessage AccessRequestCreateFailed(ILogger logger, string reason);

    /// <summary>
    /// Logged at Trace level when listing access requests.
    /// </summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "Listing access requests for user '{userId}'")]
    public static partial IGenericMessage ListingAccessRequests(ILogger logger, string userId);

    /// <summary>
    /// Logged at Information level when access requests are listed.
    /// </summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Information, Message = "Found {count} access requests")]
    public static partial IGenericMessage AccessRequestsListed(ILogger logger, int count);

    /// <summary>
    /// Logged at Error level when listing access requests fails.
    /// </summary>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error, Message = "Failed to list access requests: {reason}")]
    public static partial IGenericMessage AccessRequestListFailed(ILogger logger, string reason);

    /// <summary>
    /// Logged at Trace level when approving an access request.
    /// </summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Approving access request '{requestId}'")]
    public static partial IGenericMessage ApprovingAccessRequest(ILogger logger, string requestId);

    /// <summary>
    /// Logged at Information level when an access request is approved.
    /// </summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information, Message = "Access request '{requestId}' approved")]
    public static partial IGenericMessage AccessRequestApproved(ILogger logger, string requestId);

    /// <summary>
    /// Logged at Error level when approving an access request fails.
    /// </summary>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error, Message = "Failed to approve access request '{requestId}': {reason}")]
    public static partial IGenericMessage AccessRequestApproveFailed(ILogger logger, string requestId, string reason);

    /// <summary>
    /// Logged at Trace level when denying an access request.
    /// </summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Denying access request '{requestId}'")]
    public static partial IGenericMessage DenyingAccessRequest(ILogger logger, string requestId);

    /// <summary>
    /// Logged at Information level when an access request is denied.
    /// </summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Information, Message = "Access request '{requestId}' denied")]
    public static partial IGenericMessage AccessRequestDenied(ILogger logger, string requestId);

    /// <summary>
    /// Logged at Error level when denying an access request fails.
    /// </summary>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error, Message = "Failed to deny access request '{requestId}': {reason}")]
    public static partial IGenericMessage AccessRequestDenyFailed(ILogger logger, string requestId, string reason);

    // ═══════════════════════════════════════════════════════════════════════════
    // Conversation send operations
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace level when sending a message into a conversation thread.
    /// </summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Sending message into thread '{referenceId}'")]
    public static partial IGenericMessage SendingMessage(ILogger logger, string referenceId);

    /// <summary>
    /// Logged at Information level when a message was sent into a conversation thread.
    /// </summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Information, Message = "Message '{messageId}' sent into thread '{referenceId}'")]
    public static partial IGenericMessage MessageSent(ILogger logger, string messageId, string referenceId);

    /// <summary>
    /// Logged at Error level when sending a message into a conversation thread fails.
    /// </summary>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error, Message = "Failed to send message into thread '{referenceId}': {reason}")]
    public static partial IGenericMessage MessageSendFailed(ILogger logger, string referenceId, string reason);

    /// <summary>
    /// Logged at Warning level when a send is refused for carrying no thread reference.
    /// </summary>
    [MessageLogging(EventId = 51002, Level = LogLevel.Warning, Message = "Refused send with no ReferenceId — the thread is not identified")]
    public static partial IGenericMessage ReferenceIdMissing(ILogger logger);

    /// <summary>
    /// Logged at Warning level when a send is refused for carrying no subject.
    /// </summary>
    [MessageLogging(EventId = 51003, Level = LogLevel.Warning, Message = "Refused send into thread '{referenceId}' with no subject")]
    public static partial IGenericMessage SubjectMissing(ILogger logger, string referenceId);

    /// <summary>
    /// Logged at Warning level when a send is refused because the declared message type is not a
    /// conversation type.
    /// </summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning, Message = "Refused message type '{messageType}' — not a conversation type")]
    public static partial IGenericMessage MessageTypeRefused(ILogger logger, string messageType);

    /// <summary>
    /// Logged at Warning level when user identity claim is not found.
    /// </summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "User identity claim not found in request")]
    public static partial IGenericMessage UserClaimNotFound(ILogger logger);

    /// <summary>
    /// Logged at Error level when a messaging endpoint encounters an exception.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Messaging endpoint exception during '{operation}'")]
    public static partial IGenericMessage MessagingException(ILogger logger, Exception ex, string operation);
}
