using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.GetBaselineTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class GetBaselineTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11048,
        Level = LogLevel.Trace,
        Message = "GetBaselineTranslator translating GetBaselineCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs whether a baseline is currently set.</summary>
    [MessageLogging(
        EventId = 12000,
        Level = LogLevel.Debug,
        Message = "GetBaselineTranslator: HasBaseline={hasBaseline}")]
    public static partial IGenericMessage BaselineState(
        ILogger logger,
        bool hasBaseline);

    /// <summary>Logs completion with baseline status.</summary>
    [MessageLogging(
        EventId = 13006,
        Level = LogLevel.Information,
        Message = "GetBaselineTranslator: {message}")]
    public static partial IGenericMessage BaselineReturned(
        ILogger logger,
        string message);
}
