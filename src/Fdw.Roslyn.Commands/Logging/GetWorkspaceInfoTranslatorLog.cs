using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Workspace.Translators.GetWorkspaceInfoTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetWorkspaceInfoTranslatorLog
{
    /// <summary>Trace: workspace info retrieval starting.</summary>
    [MessageLogging(EventId = 11171, Level = LogLevel.Trace,
        Message = "GetWorkspaceInfoTranslator retrieving workspace info")]
    public static partial IGenericMessage Getting(ILogger logger);

    /// <summary>Information: retrieval completed.</summary>
    [MessageLogging(EventId = 11172, Level = LogLevel.Information,
        Message = "GetWorkspaceInfoTranslator retrieved workspace info: {projectCount} project(s), {documentCount} document(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, int projectCount, int documentCount);
}
