using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.AddDocumentTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AddDocumentTranslatorLog
{
    /// <summary>Trace: document add starting.</summary>
    [MessageLogging(EventId = 11100, Level = LogLevel.Trace,
        Message = "AddDocumentTranslator adding '{documentName}' to project '{projectName}'")]
    public static partial IGenericMessage Adding(ILogger logger, string projectName, string documentName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "AddDocumentTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Debug: the document already exists, so the add was a no-op.</summary>
    [MessageLogging(EventId = 11101, Level = LogLevel.Debug,
        Message = "AddDocumentTranslator: '{documentName}' already exists in project '{projectName}'")]
    public static partial IGenericMessage AlreadyExists(ILogger logger, string projectName, string documentName);

    /// <summary>Information: the document was added.</summary>
    [MessageLogging(EventId = 11102, Level = LogLevel.Information,
        Message = "AddDocumentTranslator added '{documentName}' to project '{projectName}'")]
    public static partial IGenericMessage Added(ILogger logger, string projectName, string documentName);
}
