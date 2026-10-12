using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GenerateClassTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GenerateClassTranslatorLog
{
    /// <summary>Trace: class generation starting.</summary>
    [MessageLogging(EventId = 11070, Level = LogLevel.Trace,
        Message = "GenerateClassTranslator generating class '{className}' in namespace '{namespaceName}'")]
    public static partial IGenericMessage Generating(ILogger logger, string className, string namespaceName);

    /// <summary>Error: ClassName was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ClassNameRequired</c> (20000).</remarks>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "GenerateClassTranslator: ClassName is required")]
    public static partial IGenericMessage ClassNameRequired(ILogger logger);

    /// <summary>Error: Namespace was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NamespaceRequired</c> (21007).</remarks>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error,
        Message = "GenerateClassTranslator: Namespace is required")]
    public static partial IGenericMessage NamespaceRequired(ILogger logger);

    /// <summary>Error: the named target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "GenerateClassTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: no target project was supplied and the solution has no projects.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoProjectsFoundInSolution</c> (31006).</remarks>
    [MessageLogging(EventId = 31006, Level = LogLevel.Error,
        Message = "GenerateClassTranslator: no projects found in solution")]
    public static partial IGenericMessage NoProjectsFoundInSolution(ILogger logger);

    /// <summary>Error: a document with the target file name exists but could not be reloaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadExistingDocument</c> (91010).</remarks>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error,
        Message = "GenerateClassTranslator: failed to load existing document '{fileName}'")]
    public static partial IGenericMessage FailedToLoadExistingDocument(ILogger logger, string fileName);

    /// <summary>Information: the class was generated.</summary>
    [MessageLogging(EventId = 11071, Level = LogLevel.Information,
        Message = "GenerateClassTranslator generated class '{className}' in namespace '{namespaceName}'")]
    public static partial IGenericMessage Generated(ILogger logger, string className, string namespaceName);
}
