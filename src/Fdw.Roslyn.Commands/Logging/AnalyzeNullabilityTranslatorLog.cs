using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.AnalyzeNullabilityTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AnalyzeNullabilityTranslatorLog
{
    /// <summary>Trace: nullability analysis starting.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "AnalyzeNullabilityTranslator analyzing '{filePath}'")]
    public static partial IGenericMessage Analyzing(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "AnalyzeNullabilityTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "AnalyzeNullabilityTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "AnalyzeNullabilityTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Information: nullability analysis completed.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "AnalyzeNullabilityTranslator analyzed {total} symbol(s) in '{filePath}': {nullable} nullable, {nonNullable} non-nullable")]
    public static partial IGenericMessage Analyzed(ILogger logger, string filePath, int total, int nullable, int nonNullable);
}
