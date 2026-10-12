using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.ApplyNamingConventionsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ApplyNamingConventionsTranslatorLog
{
    /// <summary>Trace: naming-convention check starting.</summary>
    [MessageLogging(EventId = 11055, Level = LogLevel.Trace,
        Message = "ApplyNamingConventionsTranslator checking '{filePath}' (useAsyncSuffix={useAsyncSuffix})")]
    public static partial IGenericMessage Checking(ILogger logger, string filePath, bool useAsyncSuffix);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "ApplyNamingConventionsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "ApplyNamingConventionsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "ApplyNamingConventionsTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Information: the naming-convention check completed.</summary>
    [MessageLogging(EventId = 11056, Level = LogLevel.Information,
        Message = "ApplyNamingConventionsTranslator found {violationCount} naming convention violation(s) in '{filePath}'")]
    public static partial IGenericMessage Checked(ILogger logger, string filePath, int violationCount);
}
