using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Endpoints.Logging;

/// <summary>
/// High-performance MessageLogging for notification endpoint operations.
/// EventId range: 4250-4299
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS8")]
public static partial class NotificationEndpointLog
{
    /// <summary>Logs when listing notifications.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Listing notifications")]
    public static partial IGenericMessage ListingNotifications(ILogger logger);

    /// <summary>Logs when getting a notification by name.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Getting notification '{notificationName}'")]
    public static partial IGenericMessage GettingNotification(ILogger logger, string notificationName);

    /// <summary>Logs when a notification is not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Notification '{notificationName}' not found")]
    public static partial IGenericMessage NotificationNotFound(ILogger logger, string notificationName);

    /// <summary>Logs when listing notification rules.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Listing notification rules")]
    public static partial IGenericMessage ListingNotificationRules(ILogger logger);

    /// <summary>Logs when getting a notification rule by name.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Getting notification rule '{ruleName}'")]
    public static partial IGenericMessage GettingNotificationRule(ILogger logger, string ruleName);

    /// <summary>Logs when a notification rule is not found.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning,
        Message = "Notification rule '{ruleName}' not found")]
    public static partial IGenericMessage NotificationRuleNotFound(ILogger logger, string ruleName);

    /// <summary>Logs when listing user notification preferences.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Listing notification preferences for user '{userId}'")]
    public static partial IGenericMessage ListingUserPreferences(ILogger logger, string userId);

    /// <summary>Logs when updating user notification preferences.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Updated notification preferences for user '{userId}'")]
    public static partial IGenericMessage UpdatedUserPreferences(ILogger logger, string userId);

    /// <summary>Logs when listing notification lists.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "Listing notification lists")]
    public static partial IGenericMessage ListingNotificationLists(ILogger logger);

    /// <summary>Logs when getting a notification list by name.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace,
        Message = "Getting notification list '{listName}'")]
    public static partial IGenericMessage GettingNotificationList(ILogger logger, string listName);

    /// <summary>Logs when a notification list is not found.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning,
        Message = "Notification list '{listName}' not found")]
    public static partial IGenericMessage NotificationListNotFound(ILogger logger, string listName);

    /// <summary>Logs when listing notification recipients for a rule.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "Listing notification recipients for rule '{ruleName}'")]
    public static partial IGenericMessage ListingNotificationRecipients(ILogger logger, string ruleName);

    /// <summary>Logs when listing notification conditions for a rule.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "Listing notification conditions for rule '{ruleName}'")]
    public static partial IGenericMessage ListingNotificationConditions(ILogger logger, string ruleName);

    /// <summary>Logs when listing notification list members.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "Listing members for notification list '{listName}'")]
    public static partial IGenericMessage ListingNotificationListMembers(ILogger logger, string listName);

    /// <summary>Logs when user preferences are not found.</summary>
    // Why: level raised to Warning but the "returning defaults" behavior itself is intentionally
    // left in place pending a separate decision.
    [MessageLogging(EventId = 11011, Level = LogLevel.Warning,
        Message = "No notification preferences found for user '{userId}', returning defaults")]
    public static partial IGenericMessage UserPreferencesNotFound(ILogger logger, string userId);
}
