using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Guides.GuideTargetResolver"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GuideTargetResolverLog
{
    /// <summary>Trace: the target is being resolved by type.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Trace,
        Message = "GuideTargetResolver resolving type '{typeName}' across the solution")]
    public static partial IGenericMessage ResolvingType(ILogger logger, string typeName);

    /// <summary>Trace: the target is being resolved by project.</summary>
    [MessageLogging(EventId = 10002, Level = LogLevel.Trace,
        Message = "GuideTargetResolver resolving project '{projectName}'")]
    public static partial IGenericMessage ResolvingProject(ILogger logger, string projectName);

    /// <summary>Trace: the target is being resolved by type within a project.</summary>
    [MessageLogging(EventId = 10003, Level = LogLevel.Trace,
        Message = "GuideTargetResolver resolving type '{typeName}' in project '{projectName}'")]
    public static partial IGenericMessage ResolvingTypeInProject(ILogger logger, string typeName, string projectName);

    /// <summary>Error: the project is not in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "GuideTargetResolver: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the project's compilation could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetCompilation</c> (91004).</remarks>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "GuideTargetResolver: failed to get compilation for project '{projectName}'")]
    public static partial IGenericMessage FailedToGetCompilation(ILogger logger, string projectName);

    /// <summary>Error: the type is not declared in or referenced by the searched projects.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.TypeNotFound</c> (31036).</remarks>
    [MessageLogging(EventId = 31036, Level = LogLevel.Error,
        Message = "GuideTargetResolver: type '{typeName}' not found in the loaded solution")]
    public static partial IGenericMessage TypeNotFound(ILogger logger, string typeName);
}
