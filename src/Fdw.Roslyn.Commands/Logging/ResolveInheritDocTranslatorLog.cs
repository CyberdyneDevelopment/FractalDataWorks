using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.ResolveInheritDocTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ResolveInheritDocTranslatorLog
{
    /// <summary>Trace: inheritdoc resolution starting.</summary>
    [MessageLogging(EventId = 11143, Level = LogLevel.Trace,
        Message = "ResolveInheritDocTranslator resolving inheritdoc (filePath='{filePath}', projectName='{projectName}')")]
    public static partial IGenericMessage Resolving(ILogger logger, string filePath, string projectName);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "ResolveInheritDocTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the named target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "ResolveInheritDocTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Information: resolution completed.</summary>
    [MessageLogging(EventId = 11144, Level = LogLevel.Information,
        Message = "ResolveInheritDocTranslator resolved {sitesResolved} inheritdoc site(s) across {fileCount} file(s); {unresolvedCount} unresolved")]
    public static partial IGenericMessage Resolved(ILogger logger, int sitesResolved, int fileCount, int unresolvedCount);
}
