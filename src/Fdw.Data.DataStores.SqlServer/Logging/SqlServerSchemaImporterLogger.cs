using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.DataStores.SqlServer.Logging;

/// <summary>
/// Static logger class for SqlServerSchemaImporter operations using MessageLogging infrastructure.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("SQLSERVER")]
public static partial class SqlServerSchemaImporterLogger
{
    /// <summary>
    /// Logs when SQL Server schema import starts.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Starting SQL Server schema import from server: {server}, database: {database}")]
    public static partial IGenericMessage ImportStarted(
        ILogger logger,
        string server,
        string database);

    /// <summary>
    /// Logs when SQL Server schema import completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="database">The database name.</param>
    /// <param name="objectCount">Number of objects imported.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Completed SQL Server schema import for database '{database}': {objectCount} objects imported")]
    public static partial IGenericMessage ImportCompleted(
        ILogger logger,
        string database,
        int objectCount);

    /// <summary>
    /// Logs when SQL Server schema import fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "SQL Server schema import failed")]
    public static partial IGenericMessage ImportFailed(
        ILogger logger,
        Exception exception);

    /// <summary>
    /// Logs when a table is skipped during import.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="table">The table name.</param>
    /// <param name="reason">The reason for skipping.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "Skipping table {schema}.{table}: {reason}")]
    public static partial IGenericMessage TableSkipped(
        ILogger logger,
        string schema,
        string table,
        string? reason);

    /// <summary>
    /// Logs when an error occurs processing a table.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="table">The table name.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Error processing table {schema}.{table}")]
    public static partial IGenericMessage TableError(
        ILogger logger,
        string schema,
        string table,
        Exception exception);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "{message}: {reason}")]
    public static partial IGenericMessage Warning(
        ILogger logger,
        string message,
        string? reason);

    /// <summary>
    /// Logs when the extended-properties query fails (non-fatal; callers decide whether to treat it as an error).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A generic message for logging.</returns>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Warning,
        Message = "Extended properties query failed — extended property descriptions will be unavailable")]
    public static partial IGenericMessage ExtendedPropertiesFailed(
        ILogger logger,
        Exception exception);
}
