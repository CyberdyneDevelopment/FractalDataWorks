using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Components.Logging;

/// <summary>
/// MessageLogging for <see cref="Fdw.UI.Components.Services.DurationFormatter"/> operations.
/// EventId range: 11021-11024.
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS3")]
public static partial class DurationFormatterLog
{
    /// <summary>Logs when the start/end overload receives no end timestamp and short-circuits to the em dash.</summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Trace,
        Message = "Formatting a duration with no end timestamp (start={start}); returning em dash")]
    public static partial IGenericMessage FormattingIncompleteRange(ILogger logger, DateTimeOffset start);

    /// <summary>Logs entry to the start/end <c>Format</c> overload once both timestamps are present.</summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Trace,
        Message = "Formatting duration from {start} to {end}")]
    public static partial IGenericMessage FormattingRange(ILogger logger, DateTimeOffset start, DateTimeOffset end);

    /// <summary>Logs entry to the <c>Format(TimeSpan)</c> overload.</summary>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Trace,
        Message = "Formatting duration {duration}")]
    public static partial IGenericMessage FormattingDuration(ILogger logger, TimeSpan duration);

    /// <summary>Logs the human-readable string resolved for a duration.</summary>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Debug,
        Message = "Duration {duration} formatted as '{result}'")]
    public static partial IGenericMessage FormattedDuration(ILogger logger, TimeSpan duration, string result);
}
