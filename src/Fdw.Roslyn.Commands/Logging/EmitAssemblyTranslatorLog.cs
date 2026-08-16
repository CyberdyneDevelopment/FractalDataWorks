using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.EmitAssemblyTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class EmitAssemblyTranslatorLog
{
    /// <summary>Trace: emit starting.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace,
        Message = "EmitAssemblyTranslator emitting project '{projectName}' to '{outputPath}'")]
    public static partial IGenericMessage Emitting(ILogger logger, string projectName, string outputPath);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "EmitAssemblyTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the project's compilation could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetCompilation</c> (91004).</remarks>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "EmitAssemblyTranslator: failed to get compilation for project '{projectName}'")]
    public static partial IGenericMessage FailedToGetCompilation(ILogger logger, string projectName);

    /// <summary>Warning: the compilation has errors, so no assembly can be emitted.</summary>
    /// <remarks>
    /// A recovered, non-throwing outcome — the translator reports it as a successful QueryResult whose
    /// data carries Success=false, which is why this is Warning rather than Error.
    /// </remarks>
    [MessageLogging(EventId = 41100, Level = LogLevel.Warning,
        Message = "EmitAssemblyTranslator: project '{projectName}' has {errorCount} compile error(s), emit skipped")]
    public static partial IGenericMessage CompilationHasErrors(ILogger logger, string projectName, int errorCount);

    /// <summary>Warning: the emit itself reported failure.</summary>
    [MessageLogging(EventId = 41101, Level = LogLevel.Warning,
        Message = "EmitAssemblyTranslator: emit to '{outputPath}' failed with {errorCount} error(s)")]
    public static partial IGenericMessage EmitFailed(ILogger logger, string outputPath, int errorCount);

    /// <summary>Information: the assembly was emitted.</summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Information,
        Message = "EmitAssemblyTranslator emitted assembly to '{outputPath}'")]
    public static partial IGenericMessage Emitted(ILogger logger, string outputPath);
}
