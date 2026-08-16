using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.GetCompilationOptionsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetCompilationOptionsTranslatorLog
{
    /// <summary>Trace: options retrieval starting.</summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Trace,
        Message = "GetCompilationOptionsTranslator retrieving options for project '{projectName}'")]
    public static partial IGenericMessage Retrieving(ILogger logger, string projectName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "GetCompilationOptionsTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the project's compilation could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetCompilation</c> (91004).</remarks>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "GetCompilationOptionsTranslator: failed to get compilation for project '{projectName}'")]
    public static partial IGenericMessage FailedToGetCompilation(ILogger logger, string projectName);

    /// <summary>Information: options retrieval completed.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Information,
        Message = "GetCompilationOptionsTranslator retrieved options for project '{projectName}'")]
    public static partial IGenericMessage Retrieved(ILogger logger, string projectName);
}
