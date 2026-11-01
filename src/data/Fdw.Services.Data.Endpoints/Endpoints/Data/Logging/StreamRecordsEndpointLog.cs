using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for streaming record endpoints.
/// EventId range: 11200-11299 (trace/info), 31200-31299 (warning), 91200-91299 (error).
/// </summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class StreamRecordsEndpointLog
{
    /// <summary>Logs the format chosen for a stream.</summary>
    [MessageLogging(EventId = 11200, Level = LogLevel.Debug, Message = "Streaming '{resource}' as '{format}' ({mediaType})")]
    public static partial IGenericMessage FormatNegotiated(ILogger logger, string resource, string format, string mediaType);

    /// <summary>Logs that the cursor was opened and the response is about to start.</summary>
    [MessageLogging(EventId = 11201, Level = LogLevel.Trace, Message = "Opened record cursor for '{resource}' with {fieldCount} fields")]
    public static partial IGenericMessage CursorOpened(ILogger logger, string resource, int fieldCount);

    /// <summary>Logs a completed stream.</summary>
    [MessageLogging(EventId = 11202, Level = LogLevel.Information, Message = "Streamed {rowCount} rows ({byteCount} bytes) from '{resource}'")]
    public static partial IGenericMessage StreamCompleted(ILogger logger, string resource, long rowCount, long byteCount);

    /// <summary>Logs that no registered format serves the requested media type.</summary>
    [MessageLogging(EventId = 31200, Level = LogLevel.Warning, Message = "No registered record format serves Accept '{acceptHeader}' for '{resource}'")]
    public static partial IGenericMessage FormatNotAcceptable(ILogger logger, string resource, string acceptHeader);

    /// <summary>Logs that the concurrent-stream limit refused a request.</summary>
    [MessageLogging(EventId = 31201, Level = LogLevel.Warning, Message = "Refused a stream of '{resource}': {activeStreams} of {maxStreams} concurrent streams already running")]
    public static partial IGenericMessage ConcurrencyLimitReached(ILogger logger, string resource, int activeStreams, int maxStreams);

    /// <summary>Logs that the row ceiling stopped a stream before the source was exhausted.</summary>
    [MessageLogging(EventId = 31202, Level = LogLevel.Warning, Message = "Stream of '{resource}' stopped at the {maxRows} row ceiling with rows remaining")]
    public static partial IGenericMessage RowCeilingReached(ILogger logger, string resource, long maxRows);

    /// <summary>Logs that the client went away mid-stream.</summary>
    [MessageLogging(EventId = 31203, Level = LogLevel.Warning, Message = "Client disconnected after {rowCount} rows of '{resource}'")]
    public static partial IGenericMessage ClientDisconnected(ILogger logger, System.Exception exception, string resource, long rowCount);

    /// <summary>Logs that a stream was answered with the failure of opening its records, before any byte was written.</summary>
    // Why Information: whatever refused or failed the open already logged why, at that failure's own level. This line
    // only records that the endpoint answered it once, with that status.
    [MessageLogging(EventId = 11203, Level = LogLevel.Information, Message = "Stream of '{resource}' was not started: opening its records failed, and that failure is the answer")]
    public static partial IGenericMessage StreamNotStarted(ILogger logger, string resource);

    /// <summary>Logs a per-record failure raised after the response had already started.</summary>
    [MessageLogging(EventId = 91201, Level = LogLevel.Error, Message = "Stream of '{resource}' failed after {rowCount} rows: {reason}")]
    public static partial IGenericMessage StreamFailedMidResponse(ILogger logger, string resource, long rowCount, string reason);
}
