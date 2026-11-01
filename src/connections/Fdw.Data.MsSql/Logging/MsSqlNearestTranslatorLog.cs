using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// MessageLogging for <see cref="MsSqlNearestTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlNearestTranslatorLog
{
    // Why Trace: once per search, before anything is checked.
    /// <summary>A nearest search is being translated.</summary>
    [MessageLogging(EventId = 12086, Level = LogLevel.Trace,
        Message = "MsSqlNearestTranslator translating a nearest search of container '{container}' on field '{field}'")]
    public static partial IGenericMessage Translating(ILogger logger, string container, string field);

    // Why Debug: a search's statement is built, as often as people search; the milestone is the search's answer, not this.
    /// <summary>A nearest search's statement was built.</summary>
    [MessageLogging(EventId = 12087, Level = LogLevel.Debug,
        Message = "MsSqlNearestTranslator built a {metric} search of container '{container}' for the nearest {take} of {columns} columns")]
    public static partial IGenericMessage Translated(ILogger logger, string container, string metric, int take, int columns);

    /// <summary>The search came with no container.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "MsSqlNearestTranslator received a null container")]
    public static partial IGenericMessage ContainerNull(ILogger logger);

    /// <summary>The container is not reached by a database path.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "MsSqlNearestTranslator: container '{container}' path is not a database path")]
    public static partial IGenericMessage InvalidContainerPath(ILogger logger, string container);

    // Why Warning: the caller's search cannot run as asked; nothing is sent, and the reason goes back with the refusal.
    /// <summary>The search cannot be built as asked.</summary>
    [MessageLogging(EventId = 21012, Level = LogLevel.Warning,
        Message = "MsSqlNearestTranslator: the nearest search of container '{container}' is refused: {reason}")]
    public static partial IGenericMessage InvalidOptions(ILogger logger, string container, string reason);

    /// <summary>Building the statement threw.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error,
        Message = "MsSqlNearestTranslator failed to build the nearest search of container '{container}': {errorMessage}")]
    public static partial IGenericMessage TranslationFailed(ILogger logger, Exception exception, string container, string errorMessage);
}
