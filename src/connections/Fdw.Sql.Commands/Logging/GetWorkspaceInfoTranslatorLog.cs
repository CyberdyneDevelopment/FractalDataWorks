using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.GetWorkspaceInfoTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class GetWorkspaceInfoTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11049,
        Level = LogLevel.Trace,
        Message = "GetWorkspaceInfoTranslator translating GetWorkspaceInfoCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs a successfully assembled workspace summary.</summary>
    [MessageLogging(
        EventId = 13010,
        Level = LogLevel.Information,
        Message = "GetWorkspaceInfoTranslator: workspace '{projectPath}' has {scriptCount} script(s), HasBaseline={hasBaseline}")]
    public static partial IGenericMessage WorkspaceSummarized(
        ILogger logger,
        string projectPath,
        int scriptCount,
        bool hasBaseline);
}
