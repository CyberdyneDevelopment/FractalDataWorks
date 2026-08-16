using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.DetectCodeSmellsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class DetectCodeSmellsTranslatorLog
{
    /// <summary>Trace: code-smell detection starting.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "DetectCodeSmellsTranslator scanning '{filePath}'")]
    public static partial IGenericMessage Scanning(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "DetectCodeSmellsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "DetectCodeSmellsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxRoot</c> (91006).</remarks>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "DetectCodeSmellsTranslator: failed to get syntax root for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxRoot(ILogger logger, string filePath);

    /// <summary>Information: code-smell detection completed.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "DetectCodeSmellsTranslator detected {total} smell(s) in '{filePath}': {high} high, {medium} medium, {low} low")]
    public static partial IGenericMessage Detected(ILogger logger, string filePath, int total, int high, int medium, int low);
}
