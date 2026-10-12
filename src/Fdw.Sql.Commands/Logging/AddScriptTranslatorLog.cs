using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Project.Translators.AddScriptTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class AddScriptTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Trace,
        Message = "AddScriptTranslator translating AddScriptCommand for '{filePath}'")]
    public static partial IGenericMessage Translating(
        ILogger logger,
        string filePath);

    /// <summary>Logs that the command's FilePath was missing.</summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "AddScriptTranslator: FilePath is required")]
    public static partial IGenericMessage FilePathRequired(
        ILogger logger);

    /// <summary>Logs a successful in-memory script add.</summary>
    [MessageLogging(
        EventId = 13005,
        Level = LogLevel.Information,
        Message = "AddScriptTranslator added script '{filePath}'")]
    public static partial IGenericMessage ScriptAdded(
        ILogger logger,
        string filePath);
}
