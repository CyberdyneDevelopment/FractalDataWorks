using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.GetSymbolInfoTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetSymbolInfoTranslatorLog
{
    /// <summary>Trace: symbol info retrieval starting.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace,
        Message = "GetSymbolInfoTranslator retrieving symbol at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Retrieving(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetSymbolInfoTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetSymbolInfoTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GetSymbolInfoTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no symbol was found at the given position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoSymbolFoundAtLineColumn</c> (31010).</remarks>
    [MessageLogging(EventId = 31010, Level = LogLevel.Error,
        Message = "GetSymbolInfoTranslator: no symbol found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoSymbolFoundAtLineColumn(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: symbol info retrieval completed.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information,
        Message = "GetSymbolInfoTranslator retrieved info for '{symbolName}'")]
    public static partial IGenericMessage Retrieved(ILogger logger, string symbolName);
}
