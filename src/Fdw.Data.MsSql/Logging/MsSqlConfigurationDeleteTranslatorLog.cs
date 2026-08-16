using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlConfigurationDeleteTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlConfigurationDeleteTranslatorLog
{
    [MessageLogging(
        EventId = 12040,
        Level = LogLevel.Trace,
        Message = "MsSqlConfigurationDeleteTranslator translating ConfigurationDeleteCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    /// <summary>
    /// Logs the branch decision: a scoped (owner-FK) delete versus a single-row version-on-write delete.
    /// </summary>
    [MessageLogging(
        EventId = 12041,
        Level = LogLevel.Debug,
        Message = "MsSqlConfigurationDeleteTranslator: container '{container}' using {deleteKind} delete")]
    public static partial IGenericMessage ResolvedDeleteKind(
        ILogger logger,
        string container,
        string deleteKind);

    [MessageLogging(
        EventId = 12042,
        Level = LogLevel.Information,
        Message = "MsSqlConfigurationDeleteTranslator built delete statement for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container);

    // Why: reuses MsSqlDataResultCodes.ContainerNull's number (20000).
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationDeleteTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    // Why: reuses MsSqlDataResultCodes.InvalidContainerPath's number (20001).
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationDeleteTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    // Why: reuses MsSqlDataResultCodes.MissingInputData's number (21001).
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationDeleteTranslator: {commandType} for container '{container}' has no input data or the input data is not a logical Guid id")]
    public static partial IGenericMessage MissingInputData(
        ILogger logger,
        string commandType,
        string container);

    /// <summary>
    /// Logs the unrecoverable state where the container's database path declares no schema, which
    /// ConfigurationDelete requires for its schema-qualified UPDATE statement.
    /// </summary>
    [MessageLogging(
        EventId = 62002,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationDeleteTranslator: table '{table}' has no schema defined; ConfigurationDelete requires a schema-qualified table path")]
    public static partial IGenericMessage NoSchemaDefined(
        ILogger logger,
        string table);

    /// <summary>
    /// Logs the unrecoverable state where a scoped delete's owner logical FK column does not match
    /// any foreign key declared on the container.
    /// </summary>
    [MessageLogging(
        EventId = 62003,
        Level = LogLevel.Critical,
        Message = "MsSqlConfigurationDeleteTranslator: container '{container}' declares no foreign key matching logical owner column '{ownerLogicalFkColumn}'")]
    public static partial IGenericMessage UnresolvableOwnerForeignKey(
        ILogger logger,
        string container,
        string ownerLogicalFkColumn);

    // Why: reuses MsSqlDataResultCodes.DeleteTranslationFailed's number (91003).
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationDeleteTranslator failed to translate delete for container '{container}': {errorMessage}")]
    public static partial IGenericMessage DeleteTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
