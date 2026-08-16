using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlConfigurationSaveTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlConfigurationSaveTranslatorLog
{
    [MessageLogging(
        EventId = 12050,
        Level = LogLevel.Trace,
        Message = "MsSqlConfigurationSaveTranslator translating ConfigurationSaveCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 12051,
        Level = LogLevel.Information,
        Message = "MsSqlConfigurationSaveTranslator built version-on-write save for container '{container}' with {columnCount} column(s)")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container,
        int columnCount);

    // Why: reuses MsSqlDataResultCodes.ContainerNull's number (20000).
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    // Why: reuses MsSqlDataResultCodes.InvalidContainerPath's number (20001).
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    // Why: reuses MsSqlDataResultCodes.MissingInputData's number (21001).
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator: {commandType} for container '{container}' has no input data")]
    public static partial IGenericMessage MissingInputData(
        ILogger logger,
        string commandType,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the container's database path declares no schema.
    /// </summary>
    // Why: reuses the same number as MsSqlConfigurationDeleteTranslatorLog.NoSchemaDefined (62002) —
    // identical meaning (a schema-less container path), different translator.
    [MessageLogging(
        EventId = 62002,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationSaveTranslator: container '{container}' has no schema defined; ConfigurationSave requires a schema-qualified table path")]
    public static partial IGenericMessage NoSchemaDefined(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the input data's CLR type has no source-generated
    /// PocoMapper registered ([GenerateMapper] missing).
    /// </summary>
    [MessageLogging(
        EventId = 62004,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationSaveTranslator: no PocoMapper registered for type '{typeName}'; ensure it has [GenerateMapper]")]
    public static partial IGenericMessage NoPocoMapperRegistered(
        ILogger logger,
        string typeName);

    /// <summary>
    /// Logs the unrecoverable state where the mapper/container-field intersection yields no
    /// insertable columns and no resolvable foreign keys.
    /// </summary>
    [MessageLogging(
        EventId = 62005,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationSaveTranslator: no insertable columns for type '{typeName}' in container '{container}'")]
    public static partial IGenericMessage NoInsertableColumns(
        ILogger logger,
        string typeName,
        string container);

    // Why: reuses MsSqlDataResultCodes.InsertTranslationFailed's number (91005) — the save
    // translator's catch block returns the same "InsertTranslationFailed" ResultCode.
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationSaveTranslator failed to translate save for container '{container}': {errorMessage}")]
    public static partial IGenericMessage SaveTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
