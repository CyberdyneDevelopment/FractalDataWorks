using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Project.Translators.RemoveScriptTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class RemoveScriptTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Trace,
        Message = "RemoveScriptTranslator translating RemoveScriptCommand for '{filePath}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string filePath);

    /// <summary>Logs that the command's FilePath was missing.</summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "RemoveScriptTranslator: FilePath is required")]
    public static partial IGenericMessage FilePathRequired(
        ILogger logger);

    /// <summary>Logs that the script was not found in the workspace.</summary>
    [MessageLogging(
        EventId = 30000,
        Level = LogLevel.Error,
        Message = "RemoveScriptTranslator: script '{filePath}' not found in workspace")]
    public static partial IGenericMessage ScriptNotFound(
        ILogger logger,
        string filePath);

    /// <summary>Logs a successful in-memory script removal.</summary>
    [MessageLogging(
        EventId = 13004,
        Level = LogLevel.Information,
        Message = "RemoveScriptTranslator removed script '{filePath}'")]
    public static partial IGenericMessage ScriptRemoved(
        ILogger logger,
        string filePath);
}
