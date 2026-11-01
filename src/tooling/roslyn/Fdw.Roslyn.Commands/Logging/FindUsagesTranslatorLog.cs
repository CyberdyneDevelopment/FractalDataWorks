using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Search.Translators.FindUsagesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindUsagesTranslatorLog
{
    /// <summary>Trace: usage lookup starting.</summary>
    [MessageLogging(EventId = 11151, Level = LogLevel.Trace,
        Message = "FindUsagesTranslator finding usages at '{filePath}' position {position}")]
    public static partial IGenericMessage Finding(ILogger logger, string filePath, int position);

    /// <summary>Error: FilePath was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FilePathRequired</c> (21004).</remarks>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error,
        Message = "FindUsagesTranslator: FilePath is required")]
    public static partial IGenericMessage FilePathRequired(ILogger logger);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "FindUsagesTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "FindUsagesTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "FindUsagesTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no symbol was found at the given offset.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoSymbolFoundAtOffset</c> (31011).</remarks>
    [MessageLogging(EventId = 31011, Level = LogLevel.Error,
        Message = "FindUsagesTranslator: no symbol found at position {position} in '{filePath}'")]
    public static partial IGenericMessage NoSymbolFoundAtOffset(ILogger logger, string filePath, int position);

    /// <summary>Warning: SymbolFinder.FindReferencesAsync failed; usages reported as an error placeholder.</summary>
    [MessageLogging(EventId = 81101, Level = LogLevel.Warning,
        Message = "FindUsagesTranslator: FindReferencesAsync for '{symbolName}' failed ({exceptionType}); reporting empty/error placeholder")]
    public static partial IGenericMessage ReferencesLookupFailed(ILogger logger, string symbolName, string exceptionType);

    /// <summary>Information: the lookup completed.</summary>
    [MessageLogging(EventId = 11152, Level = LogLevel.Information,
        Message = "FindUsagesTranslator found {count} usage(s) of '{symbolName}'")]
    public static partial IGenericMessage Found(ILogger logger, string symbolName, int count);
}
