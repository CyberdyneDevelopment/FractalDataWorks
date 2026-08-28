using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlBulkInsertTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlBulkInsertTranslatorLog
{
    [MessageLogging(
        EventId = 12030,
        Level = LogLevel.Trace,
        Message = "MsSqlBulkInsertTranslator translating BulkInsertCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12031,
        Level = LogLevel.Information,
        Message = "MsSqlBulkInsertTranslator built SqlBulkCopy wrapper for container '{container}' with {rowCount} row(s)")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container,
        int rowCount);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlBulkInsertTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlBulkInsertTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSqlBulkInsertTranslator: {commandType} for container '{container}' has no input data")]
    public static partial IGenericMessage MissingInputData(
        ILogger logger,
        string commandType,
        string container);

    [MessageLogging(
        EventId = 21005,
        Level = LogLevel.Error,
        Message = "MsSqlBulkInsertTranslator requires IEnumerable data for container '{container}', got {actualType}")]
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
        Message = "MsSqlBulkInsertTranslator: container '{container}' has no insertable fields")]
    public static partial IGenericMessage NoInsertableFields(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "MsSqlBulkInsertTranslator failed to translate bulk insert for container '{container}': {errorMessage}")]
    public static partial IGenericMessage BulkInsertTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
