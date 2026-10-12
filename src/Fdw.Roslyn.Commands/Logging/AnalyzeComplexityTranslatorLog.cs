using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.AnalyzeComplexityTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AnalyzeComplexityTranslatorLog
{
    /// <summary>Trace: analysis starting for a document.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "AnalyzeComplexityTranslator analyzing '{filePath}' with threshold {threshold}")]
    public static partial IGenericMessage Analyzing(ILogger logger, string filePath, int threshold);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "AnalyzeComplexityTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "AnalyzeComplexityTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxRoot</c> (91006).</remarks>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "AnalyzeComplexityTranslator: failed to get syntax root for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxRoot(ILogger logger, string filePath);

    /// <summary>Information: complexity analysis completed.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "AnalyzeComplexityTranslator analyzed {methodCount} method(s) in '{filePath}', {highCount} exceed threshold {threshold}")]
    public static partial IGenericMessage Analyzed(ILogger logger, string filePath, int methodCount, int highCount, int threshold);
}
