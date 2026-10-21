using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Logging;

/// <summary>
/// MessageLogging for messaging operations.
/// </summary>
[MessageLoggingTypeCode("MESSAGING")]
public static partial class MessagingLog
{
    /// <summary>Logs message creation.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information, Message = "Message created: {subject} for user {recipientUserId}")]
    public static partial IGenericMessage MessageCreated(ILogger logger, string subject, string recipientUserId);

    /// <summary>Logs message query results.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug, Message = "Messages queried for user {userId}: {count} results")]
    public static partial IGenericMessage MessagesQueried(ILogger logger, string userId, int count);

    /// <summary>Logs message delivery.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Message {messageId} marked as delivered")]
    public static partial IGenericMessage MessageDelivered(ILogger logger, string messageId);

    /// <summary>Logs message read.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Message {messageId} marked as read")]
    public static partial IGenericMessage MessageRead(ILogger logger, string messageId);

    /// <summary>Logs message dismissal.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug, Message = "Message {messageId} dismissed")]
    public static partial IGenericMessage MessageDismissed(ILogger logger, string messageId);

    /// <summary>Logs message archival.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug, Message = "Message {messageId} archived")]
    public static partial IGenericMessage MessageArchived(ILogger logger, string messageId);

    /// <summary>Logs all messages marked as read for a user.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "All messages marked as read for user {userId}")]
    public static partial IGenericMessage AllMessagesRead(ILogger logger, string userId);

    /// <summary>Logs access request creation.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "Access request created for resource '{resource}' by user {userId}")]
    public static partial IGenericMessage AccessRequestCreated(ILogger logger, string resource, string userId);

    /// <summary>Logs access request approval.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "Access request {requestId} approved by {reviewerId}")]
    public static partial IGenericMessage AccessRequestApproved(ILogger logger, string requestId, string reviewerId);

    /// <summary>Logs access request denial.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Access request {requestId} denied by {reviewerId}")]
    public static partial IGenericMessage AccessRequestDenied(ILogger logger, string requestId, string reviewerId);

    /// <summary>Logs message creation failure.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to create message: {error}")]
    public static partial IGenericMessage MessageCreationFailed(ILogger logger, string error);

    /// <summary>Logs message query failure.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Failed to query messages: {error}")]
    public static partial IGenericMessage MessageQueryFailed(ILogger logger, string error);

    /// <summary>Logs message operation failure.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Failed to update message {messageId}: {error}")]
    public static partial IGenericMessage MessageUpdateFailed(ILogger logger, string messageId, string error);

    /// <summary>Logs access request operation failure.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to process access request {requestId}: {error}")]
    public static partial IGenericMessage AccessRequestFailed(ILogger logger, string requestId, string error);

    /// <summary>Logs that the messaging domain's configured store/path could not be resolved.</summary>
    /// <remarks>
    /// The domain row exists once no <c>ManagedConfiguration</c> row named "Messaging" is declared, or
    /// its DataStoreName/PathName is unset — either is an absence the no-fallbacks rule catches rather
    /// than defaulting to a store the deployment merely hopes exists.
    /// </remarks>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "The Messaging domain's DataStoreName/PathName could not be resolved: {error}")]
    public static partial IGenericMessage LocationNotConfigured(ILogger logger, string error);

    /// <summary>Trace entry for CreateMessage.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace, Message = "Entering CreateMessage")]
    public static partial IGenericMessage TraceCreateMessageEntry(ILogger logger);

    /// <summary>Trace entry for GetMessages.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Trace, Message = "Entering GetMessages")]
    public static partial IGenericMessage TraceGetMessagesEntry(ILogger logger);

    /// <summary>Trace entry for MarkRead.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "Entering MarkRead")]
    public static partial IGenericMessage TraceMarkReadEntry(ILogger logger);

    /// <summary>Trace entry for GetMessage.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace, Message = "Entering GetMessage")]
    public static partial IGenericMessage TraceGetMessageEntry(ILogger logger);

    /// <summary>Trace entry for GetUnreadCount.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace, Message = "Entering GetUnreadCount")]
    public static partial IGenericMessage TraceGetUnreadCountEntry(ILogger logger);

    /// <summary>Trace entry for MarkDelivered.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace, Message = "Entering MarkDelivered")]
    public static partial IGenericMessage TraceMarkDeliveredEntry(ILogger logger);

    /// <summary>Trace entry for Dismiss.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "Entering Dismiss")]
    public static partial IGenericMessage TraceDismissEntry(ILogger logger);

    /// <summary>Trace entry for Archive.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "Entering Archive")]
    public static partial IGenericMessage TraceArchiveEntry(ILogger logger);

    /// <summary>Trace entry for MarkAllRead.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Entering MarkAllRead")]
    public static partial IGenericMessage TraceMarkAllReadEntry(ILogger logger);

    /// <summary>Trace entry for RequestAccess.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Entering RequestAccess")]
    public static partial IGenericMessage TraceRequestAccessEntry(ILogger logger);

    /// <summary>Trace entry for Approve.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Entering Approve")]
    public static partial IGenericMessage TraceApproveEntry(ILogger logger);

    /// <summary>Trace entry for Deny.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Entering Deny")]
    public static partial IGenericMessage TraceDenyEntry(ILogger logger);

    /// <summary>Trace entry for GetPending.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Entering GetPending")]
    public static partial IGenericMessage TraceGetPendingEntry(ILogger logger);

    /// <summary>Trace entry for GetForUser.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Entering GetForUser")]
    public static partial IGenericMessage TraceGetForUserEntry(ILogger logger);

    /// <summary>Logs message not found.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "Paging cursor message {messageId} is not in the queried set")]
    public static partial IGenericMessage PagingCursorNotFound(ILogger logger, string messageId);

    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "After and Before cannot both be supplied")]
    public static partial IGenericMessage PagingCursorsConflict(ILogger logger);

    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Message {messageId} not found")]
    public static partial IGenericMessage MessageNotFound(ILogger logger, string messageId);

    /// <summary>Logs access request not found.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Access request {requestId} not found")]
    public static partial IGenericMessage AccessRequestNotFound(ILogger logger, string requestId);

    /// <summary>Trace entry for a UI comment operation.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Trace, Message = "Entering UI comment {operation}")]
    public static partial IGenericMessage TraceUiFeedbackEntry(ILogger logger, string operation);

    /// <summary>Logs a UI comment saved.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Information, Message = "UI comment {feedbackId} saved by {author} on {route} with urgency {urgency}")]
    public static partial IGenericMessage UiFeedbackSaved(ILogger logger, string feedbackId, string author, string route, string urgency);

    /// <summary>Logs a UI comment changed: its note edited, resolved, reopened or deleted.</summary>
    [MessageLogging(EventId = 11026, Level = LogLevel.Information, Message = "UI comment {feedbackId} {change} by {user}")]
    public static partial IGenericMessage UiFeedbackChanged(ILogger logger, string feedbackId, string change, string user);

    /// <summary>A UI comment refused because a field was not sent. A validation refusal (category 2), answered with a client error that names the field.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning, Message = "UI comment refused: {field} is required",
        ClientMessage = "{field} is required")]
    public static partial IGenericMessage UiFeedbackFieldMissing(ILogger logger, string field);

    /// <summary>A UI comment refused because a field was sent wrong. A validation refusal (category 2), answered with a client error that says what is wrong.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "UI comment refused: {field} {reason}",
        ClientMessage = "{field} {reason}")]
    public static partial IGenericMessage UiFeedbackFieldInvalid(ILogger logger, string field, string reason);

    /// <summary>A UI comment that does not exist, or is not in the caller's reach.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning, Message = "UI comment {feedbackId} not found")]
    public static partial IGenericMessage UiFeedbackNotFound(ILogger logger, string feedbackId);

    /// <summary>A UI comment that is already in the state asked for. A conflict (category 4).</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "UI comment {feedbackId} is already {state}",
        ClientMessage = "The comment is already {state}")]
    public static partial IGenericMessage UiFeedbackWrongState(ILogger logger, string feedbackId, string state);

    /// <summary>The caller has no signed-in user, name or tenant to act as. An authentication failure (category 5).</summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning, Message = "UI comment {operation} needs a signed-in caller with a user, a name and a tenant")]
    public static partial IGenericMessage UiFeedbackCallerRequired(ILogger logger, string operation);

    /// <summary>The comment is another person's, so the caller may not change or delete it. A forbidden refusal (category 10).</summary>
    [MessageLogging(EventId = 100000, Level = LogLevel.Warning, Message = "UI comment {feedbackId} belongs to another person; {user} may not {action} it",
        ClientMessage = "Only the person who left the comment can {action} it")]
    public static partial IGenericMessage UiFeedbackNotYours(ILogger logger, string feedbackId, string user, string action);

    /// <summary>The store failed a UI comment operation.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "UI comment {operation} failed in the store: {error}")]
    public static partial IGenericMessage UiFeedbackStoreFailed(ILogger logger, string operation, string? error);

    /// <summary>A UI comment operation failed in a way nothing anticipated.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "UI comment {operation} failed unexpectedly: {error}")]
    public static partial IGenericMessage UiFeedbackUnexpected(ILogger logger, string operation, string? error);

    /// <summary>Logs a UI comment acknowledged.</summary>
    [MessageLogging(EventId = 11027, Level = LogLevel.Information, Message = "UI comment {feedbackId} acknowledged by {user}")]
    public static partial IGenericMessage UiFeedbackAcknowledged(ILogger logger, string feedbackId, string user);

    /// <summary>Logs a UI comment acknowledged again by someone who already had; nothing changed.</summary>
    // Why Debug, not Information: a per-call outcome that changes nothing; Information is for what an operator would act on or audit.
    [MessageLogging(EventId = 11028, Level = LogLevel.Debug, Message = "UI comment {feedbackId} was already acknowledged by {user}; nothing changed")]
    public static partial IGenericMessage UiFeedbackAlreadyAcknowledged(ILogger logger, string feedbackId, string user);

    /// <summary>Logs the readers told about a new UI comment.</summary>
    // Why Debug, not Information: it fires on every save; the save itself is the Information record (UiFeedbackSaved).
    [MessageLogging(EventId = 11029, Level = LogLevel.Debug, Message = "UI comment {feedbackId}: {count} reader(s) told")]
    public static partial IGenericMessage UiFeedbackReadersTold(ILogger logger, string feedbackId, int count);

    /// <summary>The comment was saved but one reader could not be told.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error, Message = "UI comment {feedbackId} was saved but reader {userId} could not be told: {reason}")]
    public static partial IGenericMessage UiFeedbackReaderNotTold(ILogger logger, string feedbackId, string userId, string? reason);

    /// <summary>The comment was saved but the people to tell could not be found.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error, Message = "UI comment {feedbackId} was saved but its readers could not be found: {reason}")]
    public static partial IGenericMessage UiFeedbackReadersNotFound(ILogger logger, string feedbackId, string? reason);

    /// <summary>Logs a person agreeing with a UI comment.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Information, Message = "UI comment {feedbackId} agreed with by {user}")]
    public static partial IGenericMessage UiFeedbackAgreed(ILogger logger, string feedbackId, string user);

    /// <summary>Logs a person agreeing again with a comment they already agreed with; nothing changed.</summary>
    // Why Debug, not Information: a per-call outcome that changes nothing; Information is for what an operator would act on or audit.
    [MessageLogging(EventId = 11031, Level = LogLevel.Debug, Message = "UI comment {feedbackId} was already agreed with by {user}; nothing changed")]
    public static partial IGenericMessage UiFeedbackAlreadyAgreed(ILogger logger, string feedbackId, string user);

    /// <summary>Logs a person taking back their agreement with a UI comment.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Information, Message = "UI comment {feedbackId}: {user} took back their agreement")]
    public static partial IGenericMessage UiFeedbackAgreementTakenBack(ILogger logger, string feedbackId, string user);

    /// <summary>The person who left a comment tried to agree with it. A conflict (category 4): they are already counted by having written it.</summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Warning, Message = "UI comment {feedbackId} was left by {user}, who may not agree with it",
        ClientMessage = "You left this comment, so you cannot agree with it")]
    public static partial IGenericMessage UiFeedbackOwnComment(ILogger logger, string feedbackId, string user);

    /// <summary>Logs a comment written on a UI note.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Information, Message = "UI comment {feedbackId} commented on by {user}")]
    public static partial IGenericMessage UiFeedbackCommented(ILogger logger, string feedbackId, string user);
}
