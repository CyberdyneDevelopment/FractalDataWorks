using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.GetProjectInfoTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetProjectInfoTranslatorLog
{
    /// <summary>Trace: project info retrieval starting.</summary>
    [MessageLogging(EventId = 11106, Level = LogLevel.Trace,
        Message = "GetProjectInfoTranslator retrieving info for project '{projectName}'")]
    public static partial IGenericMessage Retrieving(ILogger logger, string projectName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "GetProjectInfoTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Information: retrieval completed.</summary>
    [MessageLogging(EventId = 11107, Level = LogLevel.Information,
        Message = "GetProjectInfoTranslator retrieved info for project '{projectName}': {documentCount} document(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, string projectName, int documentCount);
}
