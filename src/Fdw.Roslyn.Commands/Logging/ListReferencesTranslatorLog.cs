using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.ListReferencesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ListReferencesTranslatorLog
{
    /// <summary>Trace: reference listing starting.</summary>
    [MessageLogging(EventId = 11112, Level = LogLevel.Trace,
        Message = "ListReferencesTranslator listing references for project '{projectName}'")]
    public static partial IGenericMessage Listing(ILogger logger, string projectName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "ListReferencesTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Information: listing completed.</summary>
    [MessageLogging(EventId = 11113, Level = LogLevel.Information,
        Message = "ListReferencesTranslator found {count} reference(s) in project '{projectName}'")]
    public static partial IGenericMessage Listed(ILogger logger, string projectName, int count);
}
