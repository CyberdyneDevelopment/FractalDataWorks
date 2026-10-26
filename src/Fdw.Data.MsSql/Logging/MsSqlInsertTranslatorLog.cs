using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlInsertTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlInsertTranslatorLog
{
    /// <summary>
    /// Logs entry to Translate for an InsertCommand.
    /// </summary>
    [MessageLogging(
        EventId = 12010,
        Level = LogLevel.Trace,
        Message = "MsSqlInsertTranslator translating InsertCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs successful completion of an INSERT statement build.
    /// </summary>
    [MessageLogging(
        EventId = 12011,
        Level = LogLevel.Information,
        Message = "MsSqlInsertTranslator built {batchCount} batch(es) for container '{container}' covering {rowCount} row(s)")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container,
        int batchCount,
        int rowCount);

    /// <summary>Traces the bulk copy planned for an insert.</summary>
    [MessageLogging(
        EventId = 12031,
        Level = LogLevel.Information,
        Message = "MsSqlInsertTranslator planned a SqlBulkCopy for container '{container}' with {rowCount} row(s)")]
    public static partial IGenericMessage Planned(
        ILogger logger,
        string container,
        int rowCount);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator: {commandType} for container '{container}' has no input data")]
    public static partial IGenericMessage MissingInputData(
        ILogger logger,
        string commandType,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the container declares no insertable fields at all
    /// (every field is identity, computed, or system-provided).
    /// </summary>
    [MessageLogging(
        EventId = 62000,
        Level = LogLevel.Critical,
        Message = "MsSqlInsertTranslator: container '{container}' has no insertable fields")]
    public static partial IGenericMessage NoInsertableFields(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the input data object has no properties matching any
    /// insertable field on the container.
    /// </summary>
    [MessageLogging(
        EventId = 62001,
        Level = LogLevel.Critical,
        Message = "MsSqlInsertTranslator: the rows have no properties matching insertable fields for container '{container}'")]
    public static partial IGenericMessage NoMatchingProperties(
        ILogger logger,
        string container);

    /// <summary>Reports an insert whose input is not a sequence of rows.</summary>
    [MessageLogging(
        EventId = 21005,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator requires IEnumerable data for container '{container}', got {actualType}")]
    public static partial IGenericMessage InvalidDataType(
        ILogger logger,
        string container,
        string actualType);

    /// <summary>Reports an insert with no rows.</summary>
    [MessageLogging(
        EventId = 22000,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator: input collection for container '{container}' is empty")]
    public static partial IGenericMessage EmptyCollection(
        ILogger logger,
        string container);

    /// <summary>Reports rows that need more parameters than one request carries.</summary>
    [MessageLogging(
        EventId = 22001,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator: {rowCount} row(s) of {columnCount} column(s) for container '{container}' need {parameterCount} parameters; one request carries at most {maxParameters}")]
    public static partial IGenericMessage TooManyParameters(
        ILogger logger,
        string container,
        int rowCount,
        int columnCount,
        int parameterCount,
        int maxParameters);

    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "MsSqlInsertTranslator failed to translate insert for container '{container}': {errorMessage}")]
    public static partial IGenericMessage InsertTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
