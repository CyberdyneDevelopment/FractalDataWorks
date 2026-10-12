using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.CompileDocumentTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class CompileDocumentTranslatorLog
{
    /// <summary>Trace: compilation starting.</summary>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace,
        Message = "CompileDocumentTranslator compiling '{filePath}'")]
    public static partial IGenericMessage Compiling(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "CompileDocumentTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "CompileDocumentTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's containing project compilation could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetCompilation</c> (91004).</remarks>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "CompileDocumentTranslator: failed to get compilation for '{filePath}'")]
    public static partial IGenericMessage FailedToGetCompilation(ILogger logger, string filePath);

    /// <summary>Information: compilation completed.</summary>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information,
        Message = "CompileDocumentTranslator compiled '{filePath}': {errorCount} error(s), {warningCount} warning(s)")]
    public static partial IGenericMessage Compiled(ILogger logger, string filePath, int errorCount, int warningCount);
}
