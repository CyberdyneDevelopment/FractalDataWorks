using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Messaging.Components.Logging;

/// <summary>
/// MessageLogging methods for the NotificationBellProvider headless component.
/// EventId range: 4240-4249
/// </summary>
[MessageLoggingTypeCode("COMPONENTS9")]
public static partial class NotificationBellProviderLog
{
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "NotificationBellProvider: Loading unread count")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "NotificationBellProvider: Loaded unread count: {count}")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "NotificationBellProvider: Failed to load unread count")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "NotificationBellProvider: Failed to load unread count")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);

    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "NotificationBellProvider: Marking message '{messageId}' as read")]
    public static partial IGenericMessage MarkReadStarted(ILogger logger, string messageId);

    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "NotificationBellProvider: Failed to mark message '{messageId}' as read")]
    public static partial IGenericMessage MarkReadException(ILogger logger, Exception exception, string messageId);

    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "NotificationBellProvider: Marking all messages as read")]
    public static partial IGenericMessage MarkAllReadStarted(ILogger logger);

    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "NotificationBellProvider: Failed to mark all messages as read")]
    public static partial IGenericMessage MarkAllReadException(ILogger logger, Exception exception);
}
