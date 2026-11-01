using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.SetBaselineTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class SetBaselineTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11052,
        Level = LogLevel.Trace,
        Message = "SetBaselineTranslator translating SetBaselineCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs that the current state was marked as baseline.</summary>
    [MessageLogging(
        EventId = 13008,
        Level = LogLevel.Information,
        Message = "SetBaselineTranslator set baseline with {count} script(s)")]
    public static partial IGenericMessage BaselineSet(
        ILogger logger,
        int count);
}
