using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.ListProjectsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ListProjectsTranslatorLog
{
    /// <summary>Trace: project listing starting.</summary>
    [MessageLogging(EventId = 11110, Level = LogLevel.Trace,
        Message = "ListProjectsTranslator listing projects in the solution")]
    public static partial IGenericMessage Listing(ILogger logger);

    /// <summary>Information: listing completed.</summary>
    [MessageLogging(EventId = 11111, Level = LogLevel.Information,
        Message = "ListProjectsTranslator found {count} project(s)")]
    public static partial IGenericMessage Listed(ILogger logger, int count);
}
