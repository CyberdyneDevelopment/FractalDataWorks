using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications.Logging;

/// <summary>
/// Structured log messages for reading notification lists (FDW-2931).
/// </summary>
[MessageLoggingTypeCode("NOTIFICATION")]
public static partial class NotificationListLog
{
    /// <summary>Logs the lists read from the store.</summary>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Information,
        Message = "Read {count} notification lists with {members} members")]
    public static partial IGenericMessage ListsRead(ILogger logger, int count, int members);

    /// <summary>Logs that the lists or their members could not be read.</summary>
    [MessageLogging(
        EventId = 71030,
        Level = LogLevel.Error,
        Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);
}
