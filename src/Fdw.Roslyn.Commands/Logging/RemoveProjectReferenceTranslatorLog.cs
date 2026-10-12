using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.RemoveProjectReferenceTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RemoveProjectReferenceTranslatorLog
{
    /// <summary>Trace: reference removal starting.</summary>
    [MessageLogging(EventId = 11119, Level = LogLevel.Trace,
        Message = "RemoveProjectReferenceTranslator removing reference to '{referenceName}' from project '{projectName}'")]
    public static partial IGenericMessage Removing(ILogger logger, string projectName, string referenceName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "RemoveProjectReferenceTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the reference target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ReferenceProjectNotFound</c> (31016).</remarks>
    [MessageLogging(EventId = 31016, Level = LogLevel.Error,
        Message = "RemoveProjectReferenceTranslator: reference project '{referenceName}' not found")]
    public static partial IGenericMessage ReferenceProjectNotFound(ILogger logger, string referenceName);

    /// <summary>Debug: the reference did not exist, so the removal was a no-op.</summary>
    [MessageLogging(EventId = 11120, Level = LogLevel.Debug,
        Message = "RemoveProjectReferenceTranslator: reference to '{referenceName}' does not exist in project '{projectName}'")]
    public static partial IGenericMessage NotFound(ILogger logger, string projectName, string referenceName);

    /// <summary>Information: the reference was removed.</summary>
    [MessageLogging(EventId = 11121, Level = LogLevel.Information,
        Message = "RemoveProjectReferenceTranslator removed reference to '{referenceName}' from project '{projectName}'")]
    public static partial IGenericMessage Removed(ILogger logger, string projectName, string referenceName);
}
