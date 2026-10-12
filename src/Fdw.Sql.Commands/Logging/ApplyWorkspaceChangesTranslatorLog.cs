using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Workspace.Translators.ApplyWorkspaceChangesTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class ApplyWorkspaceChangesTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Trace,
        Message = "ApplyWorkspaceChangesTranslator translating ApplyWorkspaceChangesCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs that writing pending edits to disk failed.</summary>
    [MessageLogging(
        EventId = 70001,
        Level = LogLevel.Error,
        Message = "ApplyWorkspaceChangesTranslator: apply failed: {reason}")]
    public static partial IGenericMessage ApplyFailed(
        ILogger logger,
        string reason);

    /// <summary>Logs a successful write of pending edits to disk.</summary>
    [MessageLogging(
        EventId = 13009,
        Level = LogLevel.Information,
        Message = "ApplyWorkspaceChangesTranslator wrote {count} script(s) to disk")]
    public static partial IGenericMessage ChangesApplied(
        ILogger logger,
        int count);
}
