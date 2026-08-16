using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Build.Translators.ValidateSyntaxTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class ValidateSyntaxTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "ValidateSyntaxTranslator translating {commandType}")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string commandType);

    /// <summary>Logs that the translator is registered but its implementation is not yet written.</summary>
    [MessageLogging(
        EventId = 90005,
        Level = LogLevel.Error,
        Message = "ValidateSyntaxTranslator for {commandType} is registered but not yet implemented")]
    public static partial IGenericMessage NotYetImplemented(
        ILogger logger,
        string commandType);
}
