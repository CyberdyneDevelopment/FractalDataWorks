using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Notifications.Components.Logging;

/// <summary>
/// MessageLogging methods for the NotificationsProvider headless component.
/// EventId range: 4200-4239
/// Partitions: Notifications (4200-4209), Rules (4210-4219), Lists (4220-4229), Preferences (4230-4239)
/// </summary>
[MessageLoggingTypeCode("COMPONENTS11")]
public static partial class NotificationsProviderLog
{
    // ── Notifications (4200-4209) ────────────────────────────────────────────

    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "NotificationsProvider: Loading notifications")]
    public static partial IGenericMessage NotificationsLoadStarted(ILogger logger);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "NotificationsProvider: Loaded {count} notifications")]
    public static partial IGenericMessage NotificationsLoadCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Failed to load notifications")]
    public static partial IGenericMessage NotificationsLoadFailed(ILogger logger);

    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Exception loading notifications")]
    public static partial IGenericMessage NotificationsLoadException(ILogger logger, System.Exception exception);

    // ── Rules (4210-4219) ────────────────────────────────────────────────────

    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "NotificationsProvider: Loading notification rules")]
    public static partial IGenericMessage RulesLoadStarted(ILogger logger);

    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "NotificationsProvider: Loaded {count} notification rules")]
    public static partial IGenericMessage RulesLoadCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Failed to load notification rules")]
    public static partial IGenericMessage RulesLoadFailed(ILogger logger);

    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Exception loading notification rules")]
    public static partial IGenericMessage RulesLoadException(ILogger logger, System.Exception exception);

    // ── Lists (4220-4229) ────────────────────────────────────────────────────

    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "NotificationsProvider: Loading notification lists")]
    public static partial IGenericMessage ListsLoadStarted(ILogger logger);

    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "NotificationsProvider: Loaded {count} notification lists")]
    public static partial IGenericMessage ListsLoadCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71008, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Failed to load notification lists")]
    public static partial IGenericMessage ListsLoadFailed(ILogger logger);

    [MessageLogging(EventId = 71009, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Exception loading notification lists")]
    public static partial IGenericMessage ListsLoadException(ILogger logger, System.Exception exception);

    // ── Preferences (4230-4239) ──────────────────────────────────────────────

    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "NotificationsProvider: Loading preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesLoadStarted(ILogger logger, string userId);

    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "NotificationsProvider: Loaded {count} preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesLoadCompleted(ILogger logger, int count, string userId);

    [MessageLogging(EventId = 71010, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Failed to load preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesLoadFailed(ILogger logger, string userId);

    [MessageLogging(EventId = 71011, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Exception loading preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesLoadException(ILogger logger, System.Exception exception, string userId);

    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "NotificationsProvider: Saving {count} preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesSaveStarted(ILogger logger, int count, string userId);

    [MessageLogging(EventId = 11013, Level = LogLevel.Information,
        Message = "NotificationsProvider: Saved preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesSaveCompleted(ILogger logger, string userId);

    [MessageLogging(EventId = 71012, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Failed to save preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesSaveFailed(ILogger logger, string userId);

    [MessageLogging(EventId = 71013, Level = LogLevel.Warning,
        Message = "NotificationsProvider: Exception saving preferences for user '{userId}'")]
    public static partial IGenericMessage PreferencesSaveException(ILogger logger, System.Exception exception, string userId);
}
