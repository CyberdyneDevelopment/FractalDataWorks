using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.MsSqlConfigurationRetireTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlConfigurationRetireTranslatorLog
{
    [MessageLogging(
        EventId = 12040,
        Level = LogLevel.Trace,
        Message = "MsSqlConfigurationRetireTranslator translating ConfigurationRetireCommand for container '{container}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string container);

    /// <summary>Traces the retire a translation settled on: what it matches, whether it deletes, what it returns.</summary>
    [MessageLogging(
        EventId = 12041,
        Level = LogLevel.Trace,
        Message = "MsSqlConfigurationRetireTranslator: '{container}' retires the current rows matching [{matchFields}] (markDeleted={markDeleted}), returns [{returnedFields}]")]
    public static partial IGenericMessage RetirePlanned(
        ILogger logger,
        string container,
        string matchFields,
        bool markDeleted,
        string returnedFields);

    [MessageLogging(
        EventId = 12042,
        Level = LogLevel.Debug,
        Message = "MsSqlConfigurationRetireTranslator built retire statement for container '{container}'")]
    public static partial IGenericMessage Translated(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Debug,
        Message = "MsSqlConfigurationRetireTranslator: container is null")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Debug,
        Message = "MsSqlConfigurationRetireTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(
        ILogger logger,
        string container);

    /// <summary>Logged when the translator is handed a command that is not a ConfigurationRetire.</summary>
    [MessageLogging(
        EventId = 62011,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator: command type '{commandType}' is not a ConfigurationRetireCommand (container '{container}')")]
    public static partial IGenericMessage NotARetireCommand(
        ILogger logger,
        string commandType,
        string container);

    [MessageLogging(
        EventId = 62002,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator: no schema defined for container '{container}'")]
    public static partial IGenericMessage NoSchemaDefined(
        ILogger logger,
        string container);

    /// <summary>Logged when a retire matches on a key type the container does not declare.</summary>
    [MessageLogging(
        EventId = 62010,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator: container '{container}' declares no {keyType} key to retire by")]
    public static partial IGenericMessage KeyNotDeclared(
        ILogger logger,
        string container,
        string keyType);

    /// <summary>Logged when a retire's key values do not line up with the key's fields.</summary>
    [MessageLogging(
        EventId = 62016,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator: container '{container}' {keyType} key has {fieldCount} field(s) but the retire carries {valueCount} value(s)")]
    public static partial IGenericMessage KeyValueCountMismatch(
        ILogger logger,
        string container,
        string keyType,
        int fieldCount,
        int valueCount);

    /// <summary>Logged when a retire matches on a field the container does not declare.</summary>
    [MessageLogging(
        EventId = 62017,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator: container '{container}' declares no field '{field}' to retire by")]
    public static partial IGenericMessage MatchFieldNotDeclared(
        ILogger logger,
        string container,
        string field);

    /// <summary>Logged when a retire carries nothing to match on, which would retire every current row.</summary>
    [MessageLogging(
        EventId = 62018,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator: retire of '{container}' matches on no field; refusing to retire every current row")]
    public static partial IGenericMessage NothingToMatch(
        ILogger logger,
        string container);

    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "MsSqlConfigurationRetireTranslator failed to translate retire for container '{container}': {errorMessage}")]
    public static partial IGenericMessage RetireTranslationFailed(
        ILogger logger,
        System.Exception exception,
        string container,
        string errorMessage);
}
