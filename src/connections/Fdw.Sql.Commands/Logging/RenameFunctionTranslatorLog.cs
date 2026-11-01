using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Refactoring.Translators.RenameFunctionTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class RenameFunctionTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11036,
        Level = LogLevel.Trace,
        Message = "RenameFunctionTranslator translating {commandType}")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string commandType);

    /// <summary>Logs that the translator is registered but its implementation is not yet written.</summary>
    [MessageLogging(
        EventId = 90005,
        Level = LogLevel.Error,
        Message = "RenameFunctionTranslator for {commandType} is registered but not yet implemented")]
    public static partial IGenericMessage NotYetImplemented(
        ILogger logger,
        string commandType);
}
