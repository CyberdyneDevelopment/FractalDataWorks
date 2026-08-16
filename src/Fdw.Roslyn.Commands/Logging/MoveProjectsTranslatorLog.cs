using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Projects.Translators.MoveProjectsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class MoveProjectsTranslatorLog
{
    /// <summary>Trace: move computation starting.</summary>
    [MessageLogging(EventId = 11114, Level = LogLevel.Trace,
        Message = "MoveProjectsTranslator computing moves for {moveCount} project(s)")]
    public static partial IGenericMessage Computing(ILogger logger, int moveCount);

    /// <summary>Error: no moves were specified in the command.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoMovesSpecified</c> (40000).</remarks>
    [MessageLogging(EventId = 40000, Level = LogLevel.Error,
        Message = "MoveProjectsTranslator: no moves specified")]
    public static partial IGenericMessage NoMovesSpecified(ILogger logger);

    /// <summary>Error: the same project was named more than once in the batch.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DuplicateProjectInBatch</c> (41000).</remarks>
    [MessageLogging(EventId = 41000, Level = LogLevel.Error,
        Message = "MoveProjectsTranslator: project '{projectName}' appears more than once in the batch")]
    public static partial IGenericMessage DuplicateProjectInBatch(ILogger logger, string projectName);

    /// <summary>Error: a named project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "MoveProjectsTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the requested target folder is the project's current folder.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.TargetSameAsCurrent</c> (41001).</remarks>
    [MessageLogging(EventId = 41001, Level = LogLevel.Error,
        Message = "MoveProjectsTranslator: project '{projectName}' is already in the requested target folder")]
    public static partial IGenericMessage TargetSameAsCurrent(ILogger logger, string projectName);

    /// <summary>Information: move computation completed.</summary>
    [MessageLogging(EventId = 11115, Level = LogLevel.Information,
        Message = "MoveProjectsTranslator computed moves for {projectCount} project(s): {csprojCount} .csproj file(s), {slnxCount} .slnx path change(s)")]
    public static partial IGenericMessage Computed(ILogger logger, int projectCount, int csprojCount, int slnxCount);
}
