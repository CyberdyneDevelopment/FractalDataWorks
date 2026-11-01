using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Navigation.Translators.FindOverridesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindOverridesTranslatorLog
{
    /// <summary>Trace: override lookup starting.</summary>
    [MessageLogging(EventId = 11094, Level = LogLevel.Trace,
        Message = "FindOverridesTranslator finding overrides at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Finding(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "FindOverridesTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "FindOverridesTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "FindOverridesTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a method, property, or event.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolMustBeMethodPropertyOrEvent</c> (21017).</remarks>
    [MessageLogging(EventId = 21017, Level = LogLevel.Error,
        Message = "FindOverridesTranslator: symbol at {line}:{column} in '{filePath}' is not a method, property, or event")]
    public static partial IGenericMessage SymbolMustBeMethodPropertyOrEvent(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the lookup completed.</summary>
    [MessageLogging(EventId = 11095, Level = LogLevel.Information,
        Message = "FindOverridesTranslator found {count} override(s) for '{symbolName}'")]
    public static partial IGenericMessage Found(ILogger logger, string symbolName, int count);
}
