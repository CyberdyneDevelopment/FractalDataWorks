using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Logging;

/// <summary>
/// MessageLogging for the HTTP config-driven record write seam (the record writer path that
/// serializes rows through <c>RecordWriterTypes</c> and POSTs/PUTs them to an HTTP endpoint).
/// EventId range: 7170-7173.
/// </summary>
[MessageLoggingTypeCode("HTTP")]
public static partial class HttpRecordConnectorLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (7170-7171)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the HTTP record connector is writing records to a configured endpoint.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the HTTP connection.</param>
    /// <param name="count">The number of records to write.</param>
    /// <param name="endpoint">The endpoint path being written to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Trace, Message = "HTTP connection '{connectionName}' writing {count} records to endpoint '{endpoint}'")]
    public static partial IGenericMessage WritingRecords(ILogger logger, string connectionName, int count, string endpoint);

    /// <summary>
    /// Logs that the HTTP record connector finished writing records to a configured endpoint.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the HTTP connection.</param>
    /// <param name="count">The number of records written.</param>
    /// <param name="endpoint">The endpoint path written to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "HTTP connection '{connectionName}' wrote {count} records to endpoint '{endpoint}'")]
    public static partial IGenericMessage WriteRecordsCompleted(ILogger logger, string connectionName, int count, string endpoint);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error (7172-7173)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the container carries no configured record format, so no record writer can be built.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the HTTP connection.</param>
    /// <param name="container">The container that is missing a format.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61006, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': container '{container}' has no configured record format")]
    public static partial IGenericMessage FormatNotConfigured(ILogger logger, string connectionName, string container);

    /// <summary>
    /// Logs that no record writer type is registered for the container's configured format.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the HTTP connection.</param>
    /// <param name="format">The configured format that has no registered writer type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61007, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': no record writer registered for format '{format}'")]
    public static partial IGenericMessage FormatNotRegistered(ILogger logger, string connectionName, string format);

    /// <summary>
    /// Logs that the HTTP send to the configured endpoint returned a non-success status code.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="connectionName">The name of the HTTP connection.</param>
    /// <param name="endpoint">The endpoint path that returned the failure status.</param>
    /// <param name="statusCode">The HTTP status code returned by the remote endpoint.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': send to '{endpoint}' failed with status {statusCode}")]
    public static partial IGenericMessage HttpSendFailed(ILogger logger, string connectionName, string endpoint, int statusCode);
}
