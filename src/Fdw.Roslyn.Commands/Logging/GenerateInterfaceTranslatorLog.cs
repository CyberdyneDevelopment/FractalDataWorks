using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GenerateInterfaceTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GenerateInterfaceTranslatorLog
{
    /// <summary>Trace: interface generation starting.</summary>
    [MessageLogging(EventId = 11076, Level = LogLevel.Trace,
        Message = "GenerateInterfaceTranslator generating interface '{interfaceName}' in namespace '{namespaceName}'")]
    public static partial IGenericMessage Generating(ILogger logger, string interfaceName, string namespaceName);

    /// <summary>Error: InterfaceName was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.InterfaceNameRequired</c> (21005).</remarks>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error,
        Message = "GenerateInterfaceTranslator: InterfaceName is required")]
    public static partial IGenericMessage InterfaceNameRequired(ILogger logger);

    /// <summary>Error: Namespace was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NamespaceRequired</c> (21007).</remarks>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error,
        Message = "GenerateInterfaceTranslator: Namespace is required")]
    public static partial IGenericMessage NamespaceRequired(ILogger logger);

    /// <summary>Error: the named target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "GenerateInterfaceTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: no target project was supplied and the solution has no projects.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoProjectsFoundInSolution</c> (31006).</remarks>
    [MessageLogging(EventId = 31006, Level = LogLevel.Error,
        Message = "GenerateInterfaceTranslator: no projects found in solution")]
    public static partial IGenericMessage NoProjectsFoundInSolution(ILogger logger);

    /// <summary>Error: a document with the target file name exists but could not be reloaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadExistingDocument</c> (91010).</remarks>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error,
        Message = "GenerateInterfaceTranslator: failed to load existing document '{fileName}'")]
    public static partial IGenericMessage FailedToLoadExistingDocument(ILogger logger, string fileName);

    /// <summary>Information: the interface was generated.</summary>
    [MessageLogging(EventId = 11077, Level = LogLevel.Information,
        Message = "GenerateInterfaceTranslator generated interface '{interfaceName}' with {memberCount} member(s)")]
    public static partial IGenericMessage Generated(ILogger logger, string interfaceName, int memberCount);
}
