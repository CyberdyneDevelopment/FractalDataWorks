using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.Translators.MsSqlInsertFromQueryTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlInsertFromQueryTranslatorLog
{
    /// <summary>Traces a translation starting.</summary>
    [MessageLogging(EventId = 12082, Level = LogLevel.Trace,
        Message = "MsSqlInsertFromQueryTranslator translating an insert-from-query into container '{container}'")]
    public static partial IGenericMessage Translating(ILogger logger, string container);

    /// <summary>Records a translation that produced a statement.</summary>
    [MessageLogging(EventId = 12083, Level = LogLevel.Information,
        Message = "MsSqlInsertFromQueryTranslator built INSERT ... SELECT into container '{container}' with {columnCount} columns")]
    public static partial IGenericMessage Translated(ILogger logger, string container, int columnCount);

    /// <summary>Records a null container.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "MsSqlInsertFromQueryTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    /// <summary>Records a container whose path is not a database path.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "MsSqlInsertFromQueryTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(ILogger logger, string container);

    /// <summary>Records options that cannot be used against the container.</summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Warning,
        Message = "MsSqlInsertFromQueryTranslator: the options for container '{container}' are refused: {reason}")]
    public static partial IGenericMessage InvalidOptions(ILogger logger, string container, string reason);

    /// <summary>Records an unanticipated translation error.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "MsSqlInsertFromQueryTranslator failed to translate for container '{container}': {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, System.Exception exception, string container, string errorMessage);
}
