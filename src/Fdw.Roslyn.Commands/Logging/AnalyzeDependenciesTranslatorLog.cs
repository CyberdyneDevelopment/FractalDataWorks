using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.AnalyzeDependenciesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AnalyzeDependenciesTranslatorLog
{
    /// <summary>Trace: dependency analysis starting.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "AnalyzeDependenciesTranslator analyzing '{filePath}' at {line}:{column} (includeSystemTypes={includeSystemTypes})")]
    public static partial IGenericMessage Analyzing(ILogger logger, string filePath, int line, int column, bool includeSystemTypes);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "AnalyzeDependenciesTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "AnalyzeDependenciesTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "AnalyzeDependenciesTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a named type.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolNotType</c> (21021).</remarks>
    [MessageLogging(EventId = 21021, Level = LogLevel.Error,
        Message = "AnalyzeDependenciesTranslator: symbol at {line}:{column} in '{filePath}' is not a type")]
    public static partial IGenericMessage SymbolNotType(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: dependency analysis completed.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "AnalyzeDependenciesTranslator found {dependencyCount} dependenc(y/ies) for '{typeName}'")]
    public static partial IGenericMessage Analyzed(ILogger logger, string typeName, int dependencyCount);
}
