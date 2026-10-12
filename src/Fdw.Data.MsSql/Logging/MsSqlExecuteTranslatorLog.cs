using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.Translators.MsSqlExecuteTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlExecuteTranslatorLog
{
    /// <summary>Traces a translation starting.</summary>
    [MessageLogging(EventId = 12084, Level = LogLevel.Trace,
        Message = "MsSqlExecuteTranslator translating an execute of container '{container}'")]
    public static partial IGenericMessage Translating(ILogger logger, string container);

    /// <summary>Records a translation that produced a statement.</summary>
    [MessageLogging(EventId = 12085, Level = LogLevel.Information,
        Message = "MsSqlExecuteTranslator built EXEC of container '{container}' with {parameterCount} parameters")]
    public static partial IGenericMessage Translated(ILogger logger, string container, int parameterCount);

    /// <summary>Records a null container.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "MsSqlExecuteTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    /// <summary>Records a container whose path is not a database path.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "MsSqlExecuteTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(ILogger logger, string container);

    /// <summary>Records options or a container that cannot be used.</summary>
    [MessageLogging(EventId = 21011, Level = LogLevel.Warning,
        Message = "MsSqlExecuteTranslator: the execute of container '{container}' is refused: {reason}")]
    public static partial IGenericMessage InvalidOptions(ILogger logger, string container, string reason);

    /// <summary>Records an unanticipated translation error.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "MsSqlExecuteTranslator failed to translate for container '{container}': {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, System.Exception exception, string container, string errorMessage);
}
