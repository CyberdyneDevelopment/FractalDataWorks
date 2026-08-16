using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.GetCompilationDiagnosticsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetCompilationDiagnosticsTranslatorLog
{
    /// <summary>Trace: diagnostics retrieval starting.</summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Trace,
        Message = "GetCompilationDiagnosticsTranslator retrieving diagnostics (filePath='{filePath}', projectName='{projectName}', minSeverity={minSeverity})")]
    public static partial IGenericMessage Retrieving(ILogger logger, string filePath, string projectName, string minSeverity);

    /// <summary>Error: neither FilePath nor ProjectName was supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.EitherFilePathOrProjectNameRequired</c> (21003).</remarks>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error,
        Message = "GetCompilationDiagnosticsTranslator: either FilePath or ProjectName is required")]
    public static partial IGenericMessage EitherFilePathOrProjectNameRequired(ILogger logger);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetCompilationDiagnosticsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetCompilationDiagnosticsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's semantic model could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSemanticModel</c> (91005).</remarks>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "GetCompilationDiagnosticsTranslator: failed to get semantic model for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSemanticModel(ILogger logger, string filePath);

    /// <summary>Error: the target project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ProjectNotFound</c> (31015).</remarks>
    [MessageLogging(EventId = 31015, Level = LogLevel.Error,
        Message = "GetCompilationDiagnosticsTranslator: project '{projectName}' not found")]
    public static partial IGenericMessage ProjectNotFound(ILogger logger, string projectName);

    /// <summary>Error: the project's compilation could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetCompilation</c> (91004).</remarks>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "GetCompilationDiagnosticsTranslator: failed to get compilation for project '{projectName}'")]
    public static partial IGenericMessage FailedToGetCompilation(ILogger logger, string projectName);

    /// <summary>Information: diagnostics retrieval completed.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Information,
        Message = "GetCompilationDiagnosticsTranslator found {count} diagnostic(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, int count);
}
