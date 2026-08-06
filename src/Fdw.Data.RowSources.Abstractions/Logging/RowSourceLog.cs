using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Abstractions.Logging;

/// <summary>
/// MessageLogging for row source operations.
/// EventId range: 8300-8399
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS2")]
public static partial class RowSourceLog
{
    // ======================================================================
    // Initialization (8300-8309)
    // ======================================================================

    /// <summary>
    /// Logs that a row source is initializing.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Initializing row source type '{sourceType}' with {fieldCount} fields")]
    public static partial IGenericMessage SourceInitializing(
        ILogger logger,
        string sourceType,
        int fieldCount);

    /// <summary>
    /// Logs that a row source has been initialized.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Row source type '{sourceType}' initialized in {elapsedMs:F2}ms")]
    public static partial IGenericMessage SourceInitialized(
        ILogger logger,
        string sourceType,
        double elapsedMs);

    /// <summary>
    /// Logs that a row mapper is initializing.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Initializing row mapper type '{mapperType}' with {fieldCount} fields")]
    public static partial IGenericMessage MapperInitializing(
        ILogger logger,
        string mapperType,
        int fieldCount);

    /// <summary>
    /// Logs that a row mapper has been initialized.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Row mapper type '{mapperType}' initialized in {elapsedMs:F2}ms")]
    public static partial IGenericMessage MapperInitialized(
        ILogger logger,
        string mapperType,
        double elapsedMs);

    // ======================================================================
    // Processing (8310-8319)
    // ======================================================================

    /// <summary>
    /// Logs periodic progress during row enumeration.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Processed {rowCount} rows ({errorCount} errors) in {elapsedSec:F1}s ({rowsPerSec:F0} rows/sec)")]
    public static partial IGenericMessage EnumerationProgress(
        ILogger logger,
        long rowCount,
        long errorCount,
        double elapsedSec,
        double rowsPerSec);

    /// <summary>
    /// Logs completion of row enumeration.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Enumeration complete: {rowCount} rows ({errorCount} errors) in {elapsedSec:F2}s ({rowsPerSec:F0} rows/sec)")]
    public static partial IGenericMessage EnumerationComplete(
        ILogger logger,
        long rowCount,
        long errorCount,
        double elapsedSec,
        double rowsPerSec);

    // ======================================================================
    // Errors (8320-8329)
    // ======================================================================

    /// <summary>
    /// Logs a row-level error that doesn't stop processing.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "Row {rowNumber} error: {errorMessage}")]
    public static partial IGenericMessage RowError(
        ILogger logger,
        long rowNumber,
        string errorMessage);

    /// <summary>
    /// Logs that enumeration was aborted due to max errors exceeded.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Enumeration aborted: exceeded max error count ({maxErrors}) at row {rowNumber}")]
    public static partial IGenericMessage MaxErrorsExceeded(
        ILogger logger,
        int maxErrors,
        long rowNumber);

    /// <summary>
    /// Logs that a field was not found in the source.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "Field '{fieldName}' not found in source - will be null for all rows")]
    public static partial IGenericMessage FieldNotFound(
        ILogger logger,
        string fieldName);

    /// <summary>
    /// Logs a type conversion error for a field value.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Warning,
        Message = "Type conversion error at row {rowNumber}, field '{fieldName}': {errorMessage}")]
    public static partial IGenericMessage ConversionError(
        ILogger logger,
        long rowNumber,
        string fieldName,
        string errorMessage);

    // ======================================================================
    // Creation (8350-8359)
    // ======================================================================

    /// <summary>
    /// Logs that a cursor-backed record source was created over the given number of schema fields.
    /// Emitted once at construction; per-record projection is never logged (hot path).
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="sourceType">The concrete record-source type name.</param>
    /// <param name="fieldCount">The number of fields in the flyweight schema.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "record source '{sourceType}' created over {fieldCount} fields")]
    public static partial IGenericMessage RecordSourceCreated(
        ILogger logger,
        string sourceType,
        int fieldCount);

    // ======================================================================
    // Source-specific (8330-8349)
    // ======================================================================

    /// <summary>
    /// Logs that XML parsing is starting.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Starting XML stream parsing with row element '{rowElement}'")]
    public static partial IGenericMessage XmlParsingStarted(
        ILogger logger,
        string rowElement);

    /// <summary>
    /// Logs that JSON parsing is starting.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Debug,
        Message = "Starting JSON stream parsing with array path '{arrayPath}'")]
    public static partial IGenericMessage JsonParsingStarted(
        ILogger logger,
        string arrayPath);

    /// <summary>
    /// Logs HTTP streaming pagination.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "HTTP stream page {pageNumber}: fetching {pageSize} rows from offset {offset}")]
    public static partial IGenericMessage HttpPageFetch(
        ILogger logger,
        int pageNumber,
        int pageSize,
        long offset);

    /// <summary>
    /// Logs that HTTP streaming is complete.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "HTTP streaming complete: {pageCount} pages, {totalRows} total rows")]
    public static partial IGenericMessage HttpStreamComplete(
        ILogger logger,
        int pageCount,
        long totalRows);
}
