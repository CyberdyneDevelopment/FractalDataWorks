using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <see cref="Fdw.UI.Components.Services.RelativeTimeFormatter"/> operations.
/// EventId range: 11015-11017.
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class RelativeTimeFormatterLog
{
    /// <summary>Logs when the nullable overload receives no timestamp and short-circuits to "Never".</summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "Formatting a null timestamp as 'Never'")]
    public static partial IGenericMessage FormattingNullTimestamp(ILogger logger);

    /// <summary>Logs entry to the non-nullable <c>Format(DateTimeOffset)</c> overload.</summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Trace,
        Message = "Formatting timestamp {timestamp} as a relative time string")]
    public static partial IGenericMessage FormattingTimestamp(ILogger logger, DateTimeOffset timestamp);

    /// <summary>Logs the relative-time string resolved for a timestamp.</summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Debug,
        Message = "Timestamp {timestamp} formatted as '{result}'")]
    public static partial IGenericMessage FormattedTimestamp(ILogger logger, DateTimeOffset timestamp, string result);
}
