using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Search.Translators.FindOrphanScriptsTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class FindOrphanScriptsTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Trace,
        Message = "FindOrphanScriptsTranslator translating {commandType}")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string commandType);

    /// <summary>Logs that the translator is registered but its implementation is not yet written.</summary>
    [MessageLogging(
        EventId = 90005,
        Level = LogLevel.Error,
        Message = "FindOrphanScriptsTranslator for {commandType} is registered but not yet implemented")]
    public static partial IGenericMessage NotYetImplemented(
        ILogger logger,
        string commandType);
}
