using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Sql.Commands.Logging;

/// <summary>Message logging for <see cref="Fdw.Sql.Commands.Project.Translators.GetProjectInfoTranslator"/>.</summary>
[MessageLoggingTypeCode("SQL")]
public static partial class GetProjectInfoTranslatorLog
{
    /// <summary>Logs translator entry.</summary>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Trace,
        Message = "GetProjectInfoTranslator translating GetProjectInfoCommand")]
    public static partial IGenericMessage Translating(
        ILogger logger);

    /// <summary>Logs a successfully assembled project summary.</summary>
    [MessageLogging(
        EventId = 13002,
        Level = LogLevel.Information,
        Message = "GetProjectInfoTranslator: project '{projectPath}' has {scriptCount} script(s) and {objectCount} object(s)")]
    public static partial IGenericMessage ProjectSummarized(
        ILogger logger,
        string projectPath,
        int scriptCount,
        int objectCount);
}
