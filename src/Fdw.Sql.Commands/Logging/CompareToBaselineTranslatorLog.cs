using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.CompareToBaselineTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class CompareToBaselineTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11046,
        Level = LogLevel.Trace,
        Message = "CompareToBaselineTranslator translating CompareToBaselineCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs whether a baseline is present to compare against.</summary>
    [MessageLogging(
        EventId = 12001,
        Level = LogLevel.Debug,
        Message = "CompareToBaselineTranslator: HasBaseline={hasBaseline}")]
    public static partial IGenericMessage BaselineState(
        ILogger logger,
        bool hasBaseline);

    /// <summary>Logs that no baseline was set, so no comparison was made.</summary>
    [MessageLogging(
        EventId = 13011,
        Level = LogLevel.Information,
        Message = "CompareToBaselineTranslator: no baseline set")]
    public static partial IGenericMessage NoBaselineSet(
        ILogger logger);

    /// <summary>Logs that a baseline exists but the script-level diff is not yet implemented.</summary>
    [MessageLogging(
        EventId = 81001,
        Level = LogLevel.Warning,
        Message = "CompareToBaselineTranslator: baseline is set but the script-level diff is not yet computed")]
    public static partial IGenericMessage DiffNotYetComputed(
        ILogger logger);
}
