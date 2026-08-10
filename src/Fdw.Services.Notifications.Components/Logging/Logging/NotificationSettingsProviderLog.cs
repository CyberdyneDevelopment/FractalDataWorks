using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Notifications.Components.Logging;

/// <summary>
/// MessageLogging methods for the NotificationSettingsProvider headless component.
/// EventId range: 4210-4219
/// </summary>
[MessageLoggingTypeCode("COMPONENTS11")]
public static partial class NotificationSettingsProviderLog
{
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "NotificationSettingsProvider: Loading preferences for user '{userId}'")]
    public static partial IGenericMessage LoadStarted(ILogger logger, string userId);

    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "NotificationSettingsProvider: Loaded {count} preferences for user '{userId}'")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int count, string userId);

    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "NotificationSettingsProvider: Failed to load preferences for user '{userId}'")]
    public static partial IGenericMessage LoadFailed(ILogger logger, string userId);

    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "NotificationSettingsProvider: Failed to load preferences for user '{userId}'")]
    public static partial IGenericMessage LoadException(ILogger logger, System.Exception exception, string userId);

    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "NotificationSettingsProvider: Saving {count} preferences for user '{userId}'")]
    public static partial IGenericMessage SaveStarted(ILogger logger, int count, string userId);

    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "NotificationSettingsProvider: Saved preferences for user '{userId}'")]
    public static partial IGenericMessage SaveCompleted(ILogger logger, string userId);

    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "NotificationSettingsProvider: Failed to save preferences for user '{userId}'")]
    public static partial IGenericMessage SaveFailed(ILogger logger, string userId);

    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "NotificationSettingsProvider: Failed to save preferences for user '{userId}'")]
    public static partial IGenericMessage SaveException(ILogger logger, System.Exception exception, string userId);
}
