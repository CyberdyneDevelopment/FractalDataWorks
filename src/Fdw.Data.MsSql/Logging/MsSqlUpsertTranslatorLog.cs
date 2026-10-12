using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for the MsSql upsert and bulk upsert translators.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlUpsertTranslatorLog
{
    /// <summary>Traces a translation starting.</summary>
    [MessageLogging(EventId = 12080, Level = LogLevel.Trace,
        Message = "[{translator}] translating an upsert for container '{container}'")]
    public static partial IGenericMessage Translating(ILogger logger, string translator, string container);

    /// <summary>Records a translation that produced a statement.</summary>
    [MessageLogging(EventId = 12081, Level = LogLevel.Information,
        Message = "[{translator}] built a set-based upsert for container '{container}' ({keyCount} key columns, versioned: {versioned})")]
    public static partial IGenericMessage Translated(ILogger logger, string translator, string container, int keyCount, bool versioned);

    /// <summary>Records a null container.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "[{translator}] received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger, string translator);

    /// <summary>Records a container whose path is not a database path.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "[{translator}] container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(ILogger logger, string translator, string container);

    /// <summary>Records a command that carries no source rows.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "[{translator}] the command carries no source rows for container '{container}'")]
    public static partial IGenericMessage MissingInputData(ILogger logger, string translator, string container);

    /// <summary>Records source rows that are not a collection.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error,
        Message = "[{translator}] the source rows for container '{container}' are a {actualType}, not a collection")]
    public static partial IGenericMessage InvalidDataType(ILogger logger, string translator, string container, string actualType);

    /// <summary>Records upsert options that cannot be used against the container.</summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Warning,
        Message = "[{translator}] the upsert options for container '{container}' are refused: {reason}")]
    public static partial IGenericMessage InvalidOptions(ILogger logger, string translator, string container, string reason);

    /// <summary>Records an unanticipated translation error.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error,
        Message = "[{translator}] failed to translate the upsert for container '{container}': {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, System.Exception exception, string translator, string container, string errorMessage);
}
