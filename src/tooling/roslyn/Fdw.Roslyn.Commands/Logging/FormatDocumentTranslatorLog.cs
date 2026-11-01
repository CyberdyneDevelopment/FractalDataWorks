using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.FormatDocumentTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FormatDocumentTranslatorLog
{
    /// <summary>Trace: document formatting starting.</summary>
    [MessageLogging(EventId = 11057, Level = LogLevel.Trace,
        Message = "FormatDocumentTranslator formatting '{filePath}'")]
    public static partial IGenericMessage Formatting(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "FormatDocumentTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "FormatDocumentTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Information: formatting completed.</summary>
    [MessageLogging(EventId = 11058, Level = LogLevel.Information,
        Message = "FormatDocumentTranslator formatted '{filePath}' with {changeCount} change(s)")]
    public static partial IGenericMessage Formatted(ILogger logger, string filePath, int changeCount);
}
