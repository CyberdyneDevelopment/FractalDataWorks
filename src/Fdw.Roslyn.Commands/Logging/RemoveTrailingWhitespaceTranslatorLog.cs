using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.RemoveTrailingWhitespaceTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RemoveTrailingWhitespaceTranslatorLog
{
    /// <summary>Trace: trailing-whitespace scan starting.</summary>
    [MessageLogging(EventId = 11066, Level = LogLevel.Trace,
        Message = "RemoveTrailingWhitespaceTranslator scanning '{filePath}'")]
    public static partial IGenericMessage Scanning(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "RemoveTrailingWhitespaceTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "RemoveTrailingWhitespaceTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Information: removal completed.</summary>
    [MessageLogging(EventId = 11067, Level = LogLevel.Information,
        Message = "RemoveTrailingWhitespaceTranslator removed trailing whitespace from {lineCount} line(s) in '{filePath}'")]
    public static partial IGenericMessage Removed(ILogger logger, string filePath, int lineCount);
}
