using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.FormatSelectionTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FormatSelectionTranslatorLog
{
    /// <summary>Trace: selection formatting starting.</summary>
    [MessageLogging(EventId = 11059, Level = LogLevel.Trace,
        Message = "FormatSelectionTranslator formatting '{filePath}' from {startLine} to {endLine}")]
    public static partial IGenericMessage Formatting(ILogger logger, string filePath, int startLine, int endLine);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "FormatSelectionTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "FormatSelectionTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Information: formatting completed.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Information,
        Message = "FormatSelectionTranslator formatted '{filePath}' from line {startLine} to {endLine} with {changeCount} change(s)")]
    public static partial IGenericMessage Formatted(ILogger logger, string filePath, int startLine, int endLine, int changeCount);
}
