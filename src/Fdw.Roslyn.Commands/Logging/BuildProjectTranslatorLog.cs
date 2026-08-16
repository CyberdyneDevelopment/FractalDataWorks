using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.BuildProjectTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class BuildProjectTranslatorLog
{
    /// <summary>Trace: build starting.</summary>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace,
        Message = "BuildProjectTranslator building project '{projectName}'")]
    public static partial IGenericMessage Building(ILogger logger, string projectName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "BuildProjectTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the project's compilation could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetCompilation</c> (91004).</remarks>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "BuildProjectTranslator: failed to get compilation for project '{projectName}'")]
    public static partial IGenericMessage FailedToGetCompilation(ILogger logger, string projectName);

    /// <summary>Information: the build completed.</summary>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information,
        Message = "BuildProjectTranslator built '{projectName}': {errorCount} error(s), {warningCount} warning(s)")]
    public static partial IGenericMessage Built(ILogger logger, string projectName, int errorCount, int warningCount);
}
