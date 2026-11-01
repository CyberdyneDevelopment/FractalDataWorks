using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Logging;

/// <summary>
/// Structured log messages for <c>SqlUserNotificationPreferenceService</c>.
/// Uses EventIds 7595-7600 (notification domain range 7501-7600).
/// </summary>
[MessageLoggingTypeCode("NOTIFICATION")]
public static partial class UserNotificationPreferenceLog
{
    /// <summary>Logs when preferences are being loaded for a user.</summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Loading notification preferences for user {userId}")]
    public static partial IGenericMessage LoadingPreferences(ILogger logger, Guid userId);

    /// <summary>Logs when a preference query fails.</summary>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "Failed to query notification preferences for user {userId}")]
    public static partial IGenericMessage QueryFailed(ILogger logger, Exception exception, Guid userId);

    /// <summary>Logs when preferences are being saved for a user.</summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Debug,
        Message = "Saving {count} notification preferences for user {userId}")]
    public static partial IGenericMessage SavingPreferences(ILogger logger, int count, Guid userId);

    /// <summary>Logs when a preference write fails.</summary>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Failed to persist notification preference {notificationType}/{channel} for user {userId}")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger,
        Exception exception,
        string notificationType,
        string channel,
        Guid userId);

    /// <summary>Logs when preferences are successfully persisted for a user.</summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Persisted {count} notification preferences for user {userId}")]
    public static partial IGenericMessage PreferencesPersisted(ILogger logger, int count, Guid userId);
}
