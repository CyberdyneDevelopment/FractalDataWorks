using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.GetCallHierarchyTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetCallHierarchyTranslatorLog
{
    /// <summary>Trace: call-hierarchy build starting.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "GetCallHierarchyTranslator building '{direction}' hierarchy for '{filePath}' at {line}:{column} (maxDepth={maxDepth})")]
    public static partial IGenericMessage Building(ILogger logger, string filePath, int line, int column, string direction, int maxDepth);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetCallHierarchyTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetCallHierarchyTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GetCallHierarchyTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a method.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolNotMethod</c> (21020).</remarks>
    [MessageLogging(EventId = 21020, Level = LogLevel.Error,
        Message = "GetCallHierarchyTranslator: symbol at {line}:{column} in '{filePath}' is not a method")]
    public static partial IGenericMessage SymbolNotMethod(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the hierarchy build completed.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information,
        Message = "GetCallHierarchyTranslator built '{direction}' hierarchy for '{methodName}' with {entryCount} entrie(s)")]
    public static partial IGenericMessage Built(ILogger logger, string methodName, string direction, int entryCount);
}
