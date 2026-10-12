using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Navigation.Translators.GetContainingTypeTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetContainingTypeTranslatorLog
{
    /// <summary>Trace: containing-type lookup starting.</summary>
    [MessageLogging(EventId = 11096, Level = LogLevel.Trace,
        Message = "GetContainingTypeTranslator finding containing type at '{filePath}' {line}:{column} (includeNested={includeNested})")]
    public static partial IGenericMessage Finding(ILogger logger, string filePath, int line, int column, bool includeNested);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetContainingTypeTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetContainingTypeTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GetContainingTypeTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no containing type was found by walking up the syntax tree.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoContainingTypeFoundAtPosition</c> (31002).</remarks>
    [MessageLogging(EventId = 31002, Level = LogLevel.Error,
        Message = "GetContainingTypeTranslator: no containing type found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoContainingTypeFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the lookup completed.</summary>
    [MessageLogging(EventId = 11097, Level = LogLevel.Information,
        Message = "GetContainingTypeTranslator found containing type '{typeName}' ({count} total)")]
    public static partial IGenericMessage Found(ILogger logger, string typeName, int count);
}
