using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.RevertToBaselineTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class RevertToBaselineTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11051,
        Level = LogLevel.Trace,
        Message = "RevertToBaselineTranslator translating RevertToBaselineCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs a successful revert-to-baseline with the number of scripts reverted.</summary>
    [MessageLogging(
        EventId = 13007,
        Level = LogLevel.Information,
        Message = "RevertToBaselineTranslator reverted {count} script(s) to baseline")]
    public static partial IGenericMessage Reverted(
        ILogger logger,
        int count);
}
