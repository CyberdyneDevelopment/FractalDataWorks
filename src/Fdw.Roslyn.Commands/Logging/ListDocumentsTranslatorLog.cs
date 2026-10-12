using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.ListDocumentsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ListDocumentsTranslatorLog
{
    /// <summary>Trace: document listing starting.</summary>
    [MessageLogging(EventId = 11108, Level = LogLevel.Trace,
        Message = "ListDocumentsTranslator listing documents in project '{projectName}' (pattern='{pattern}')")]
    public static partial IGenericMessage Listing(ILogger logger, string projectName, string pattern);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "ListDocumentsTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Information: listing completed.</summary>
    [MessageLogging(EventId = 11109, Level = LogLevel.Information,
        Message = "ListDocumentsTranslator found {count} document(s) in project '{projectName}'")]
    public static partial IGenericMessage Listed(ILogger logger, string projectName, int count);
}
