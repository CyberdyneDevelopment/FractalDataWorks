using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.NormalizeLineEndingsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class NormalizeLineEndingsTranslatorLog
{
    /// <summary>Trace: line-ending normalization starting.</summary>
    [MessageLogging(EventId = 11061, Level = LogLevel.Trace,
        Message = "NormalizeLineEndingsTranslator normalizing '{filePath}' to '{targetLineEnding}'")]
    public static partial IGenericMessage Normalizing(ILogger logger, string filePath, string targetLineEnding);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "NormalizeLineEndingsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "NormalizeLineEndingsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Information: normalization completed.</summary>
    [MessageLogging(EventId = 11062, Level = LogLevel.Information,
        Message = "NormalizeLineEndingsTranslator normalized {normalizedCount} line ending(s) in '{filePath}' to {lineEndingName}")]
    public static partial IGenericMessage Normalized(ILogger logger, string filePath, int normalizedCount, string lineEndingName);
}
