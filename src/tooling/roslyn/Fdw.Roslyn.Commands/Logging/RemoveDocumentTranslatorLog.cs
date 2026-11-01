using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.RemoveDocumentTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RemoveDocumentTranslatorLog
{
    /// <summary>Trace: document removal starting.</summary>
    [MessageLogging(EventId = 11116, Level = LogLevel.Trace,
        Message = "RemoveDocumentTranslator removing '{documentPath}' from project '{projectName}'")]
    public static partial IGenericMessage Removing(ILogger logger, string projectName, string documentPath);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "RemoveDocumentTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Debug: the document was not found, so the removal was a no-op.</summary>
    [MessageLogging(EventId = 11117, Level = LogLevel.Debug,
        Message = "RemoveDocumentTranslator: '{documentPath}' not found in project '{projectName}'")]
    public static partial IGenericMessage NotFound(ILogger logger, string projectName, string documentPath);

    /// <summary>Information: the document was removed.</summary>
    [MessageLogging(EventId = 11118, Level = LogLevel.Information,
        Message = "RemoveDocumentTranslator removed '{documentName}' from project '{projectName}'")]
    public static partial IGenericMessage Removed(ILogger logger, string projectName, string documentName);
}
