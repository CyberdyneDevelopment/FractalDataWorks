using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Logging;

/// <summary>
/// Structured log messages for sending a notification through a named rule.
/// </summary>
[MessageLoggingTypeCode("NOTIFICATION")]
public static partial class RuleNotificationLog
{
    /// <summary>Logs a rule that declined a notification because it is disabled or its scope does not match.</summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Notification rule '{ruleName}' declined the notification: {reason}")]
    public static partial IGenericMessage RuleDeclined(ILogger logger, string ruleName, string reason);

    /// <summary>Logs a notification sent through a rule.</summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Notification sent through rule '{ruleName}' on channel '{channelName}'")]
    public static partial IGenericMessage RuleNotificationSent(ILogger logger, string ruleName, string channelName);

    /// <summary>Logs a rule that names a notification service that cannot be resolved.</summary>
    [MessageLogging(
        EventId = 30002,
        Level = LogLevel.Warning,
        Message = "Notification rule '{ruleName}' references unresolved channel '{channelName}'")]
    public static partial IGenericMessage RuleChannelUnresolved(ILogger logger, string ruleName, string channelName);

    /// <summary>Logs a rule whose configured notification rules could not be read.</summary>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Warning,
        Message = "Notification rule '{ruleName}' could not be read: {reason}")]
    public static partial IGenericMessage RuleReadFailed(ILogger logger, string ruleName, string? reason);

    /// <summary>Logs a send through a rule that the channel failed.</summary>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Warning,
        Message = "Notification through rule '{ruleName}' on channel '{channelName}' failed: {reason}")]
    public static partial IGenericMessage RuleNotificationSendFailed(ILogger logger, string ruleName, string channelName, string? reason);
}
