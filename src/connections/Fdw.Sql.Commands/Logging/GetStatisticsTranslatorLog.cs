using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Analysis.Translators.GetStatisticsTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class GetStatisticsTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Trace,
        Message = "GetStatisticsTranslator translating {commandType}")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string commandType);

    /// <summary>Logs that the translator is registered but its implementation is not yet written.</summary>
    [MessageLogging(
        EventId = 90005,
        Level = LogLevel.Error,
        Message = "GetStatisticsTranslator for {commandType} is registered but not yet implemented")]
    public static partial IGenericMessage NotYetImplemented(
        ILogger logger,
        string commandType);
}
