using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.GetTypeHierarchyTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetTypeHierarchyTranslatorLog
{
    /// <summary>Trace: type-hierarchy retrieval starting.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "GetTypeHierarchyTranslator retrieving hierarchy for '{filePath}' at {line}:{column} (includeInterfaces={includeInterfaces})")]
    public static partial IGenericMessage Retrieving(ILogger logger, string filePath, int line, int column, bool includeInterfaces);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetTypeHierarchyTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetTypeHierarchyTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GetTypeHierarchyTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a named type.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolNotType</c> (21021).</remarks>
    [MessageLogging(EventId = 21021, Level = LogLevel.Error,
        Message = "GetTypeHierarchyTranslator: symbol at {line}:{column} in '{filePath}' is not a type")]
    public static partial IGenericMessage SymbolNotType(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: type-hierarchy retrieval completed.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "GetTypeHierarchyTranslator retrieved hierarchy for '{typeName}': {baseTypeCount} base type(s), {interfaceCount} interface(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, string typeName, int baseTypeCount, int interfaceCount);
}
