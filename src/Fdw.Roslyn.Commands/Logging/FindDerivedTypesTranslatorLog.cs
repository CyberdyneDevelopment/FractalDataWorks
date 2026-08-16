using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Navigation.Translators.FindDerivedTypesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindDerivedTypesTranslatorLog
{
    /// <summary>Trace: derived-type lookup starting.</summary>
    [MessageLogging(EventId = 11090, Level = LogLevel.Trace,
        Message = "FindDerivedTypesTranslator finding derived types at '{filePath}' {line}:{column} (transitive={transitive})")]
    public static partial IGenericMessage Finding(ILogger logger, string filePath, int line, int column, bool transitive);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "FindDerivedTypesTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "FindDerivedTypesTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "FindDerivedTypesTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a named type.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolNotType</c> (21021).</remarks>
    [MessageLogging(EventId = 21021, Level = LogLevel.Error,
        Message = "FindDerivedTypesTranslator: symbol at {line}:{column} in '{filePath}' is not a type")]
    public static partial IGenericMessage SymbolNotType(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the lookup completed.</summary>
    [MessageLogging(EventId = 11091, Level = LogLevel.Information,
        Message = "FindDerivedTypesTranslator found {count} derived type(s) for '{typeName}'")]
    public static partial IGenericMessage Found(ILogger logger, string typeName, int count);
}
