using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Navigation.Translators.FindDefinitionTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindDefinitionTranslatorLog
{
    /// <summary>Trace: definition lookup starting.</summary>
    [MessageLogging(EventId = 11088, Level = LogLevel.Trace,
        Message = "FindDefinitionTranslator finding definition at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Finding(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "FindDefinitionTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "FindDefinitionTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "FindDefinitionTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no symbol was found at the given position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoSymbolFoundAtLineColumn</c> (31010).</remarks>
    [MessageLogging(EventId = 31010, Level = LogLevel.Error,
        Message = "FindDefinitionTranslator: no symbol found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoSymbolFoundAtLineColumn(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the symbol has no in-source location.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoSourceDefinitionFound</c> (31008).</remarks>
    [MessageLogging(EventId = 31008, Level = LogLevel.Error,
        Message = "FindDefinitionTranslator: no source definition found for '{symbolName}'")]
    public static partial IGenericMessage NoSourceDefinitionFound(ILogger logger, string symbolName);

    /// <summary>Information: the lookup completed.</summary>
    [MessageLogging(EventId = 11089, Level = LogLevel.Information,
        Message = "FindDefinitionTranslator found {count} definition(s) for '{symbolName}'")]
    public static partial IGenericMessage Found(ILogger logger, string symbolName, int count);
}
