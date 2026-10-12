using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.AddBracesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AddBracesTranslatorLog
{
    /// <summary>Trace: brace addition starting.</summary>
    [MessageLogging(EventId = 11053, Level = LogLevel.Trace,
        Message = "AddBracesTranslator adding braces to '{filePath}'")]
    public static partial IGenericMessage Adding(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "AddBracesTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "AddBracesTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxRoot</c> (91006).</remarks>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "AddBracesTranslator: failed to get syntax root for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxRoot(ILogger logger, string filePath);

    /// <summary>Information: braces were added.</summary>
    [MessageLogging(EventId = 11054, Level = LogLevel.Information,
        Message = "AddBracesTranslator added braces to {changeCount} statement(s) in '{filePath}'")]
    public static partial IGenericMessage Added(ILogger logger, string filePath, int changeCount);
}
