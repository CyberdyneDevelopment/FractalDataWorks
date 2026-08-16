using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.AddProjectReferenceTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AddProjectReferenceTranslatorLog
{
    /// <summary>Trace: reference add starting.</summary>
    [MessageLogging(EventId = 11103, Level = LogLevel.Trace,
        Message = "AddProjectReferenceTranslator adding reference to '{referenceName}' in project '{projectName}'")]
    public static partial IGenericMessage Adding(ILogger logger, string projectName, string referenceName);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "AddProjectReferenceTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the reference target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ReferenceProjectNotFound</c> (31016).</remarks>
    [MessageLogging(EventId = 31016, Level = LogLevel.Error,
        Message = "AddProjectReferenceTranslator: reference project '{referenceName}' not found")]
    public static partial IGenericMessage ReferenceProjectNotFound(ILogger logger, string referenceName);

    /// <summary>Debug: the reference already exists, so the add was a no-op.</summary>
    [MessageLogging(EventId = 11104, Level = LogLevel.Debug,
        Message = "AddProjectReferenceTranslator: reference to '{referenceName}' already exists in project '{projectName}'")]
    public static partial IGenericMessage AlreadyExists(ILogger logger, string projectName, string referenceName);

    /// <summary>Information: the reference was added.</summary>
    [MessageLogging(EventId = 11105, Level = LogLevel.Information,
        Message = "AddProjectReferenceTranslator added reference to '{referenceName}' in project '{projectName}'")]
    public static partial IGenericMessage Added(ILogger logger, string projectName, string referenceName);
}
