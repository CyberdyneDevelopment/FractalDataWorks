using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlBatchInsertTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlBatchInsertTranslatorLog
{
    [MessageLogging(
        EventId = 12020,
        Level = LogLevel.Trace,
        Message = "MsSqlBatchInsertTranslator translating InsertCommand<IEnumerable> for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12021,
        Level = LogLevel.Information,
        Message = "MsSqlBatchInsertTranslator built {batchCount} batch(es) for container '{container}' covering {rowCount} row(s)")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container,
        int batchCount,
        int rowCount);

    // Why: reuses MsSqlDataResultCodes.ContainerNull's number (20000).
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlBatchInsertTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    // Why: reuses MsSqlDataResultCodes.InvalidContainerPath's number (20001).
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlBatchInsertTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    // Why: reuses MsSqlDataResultCodes.MissingInputData's number (21001).
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSqlBatchInsertTranslator: {commandType} for container '{container}' has no input data")]
    public static partial IGenericMessage MissingInputData(
        ILogger logger,
        string commandType,
        string container);

    // Why: reuses MsSqlDataResultCodes.InvalidDataType's number (21005).
    [MessageLogging(
        EventId = 21005,
        Level = LogLevel.Error,
        Message = "MsSqlBatchInsertTranslator requires IEnumerable data for container '{container}', got {actualType}")]
    public static partial IGenericMessage InvalidDataType(
        ILogger logger,
        string container,
        string actualType);

    /// <summary>
    /// Logs the unrecoverable state where the container declares no insertable fields at all.
    /// </summary>
    [MessageLogging(
        EventId = 62000,
        Level = LogLevel.Critical,
        Message = "MsSqlBatchInsertTranslator: container '{container}' has no insertable fields")]
    public static partial IGenericMessage NoInsertableFields(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the input collection has no rows.
    /// </summary>
    [MessageLogging(
        EventId = 22000,
        Level = LogLevel.Error,
        Message = "MsSqlBatchInsertTranslator: input collection for container '{container}' is empty")]
    public static partial IGenericMessage EmptyCollection(
        ILogger logger,
        string container);

    // Why: reuses MsSqlDataResultCodes.BatchInsertTranslationFailed's number (91000).
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "MsSqlBatchInsertTranslator failed to translate batch insert for container '{container}': {errorMessage}")]
    public static partial IGenericMessage BatchInsertTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
