using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GenerateDocsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GenerateDocsTranslatorLog
{
    /// <summary>Trace: documentation generation starting.</summary>
    [MessageLogging(EventId = 11074, Level = LogLevel.Trace,
        Message = "GenerateDocsTranslator generating docs for '{filePath}' (includePrivate={includePrivate})")]
    public static partial IGenericMessage Generating(ILogger logger, string filePath, bool includePrivate);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GenerateDocsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GenerateDocsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GenerateDocsTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: every eligible member already carries documentation.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoUndocumentedMembersFound</c> (31014).</remarks>
    [MessageLogging(EventId = 31014, Level = LogLevel.Error,
        Message = "GenerateDocsTranslator: no undocumented members found in '{filePath}'")]
    public static partial IGenericMessage NoUndocumentedMembersFound(ILogger logger, string filePath);

    /// <summary>Information: documentation was generated.</summary>
    [MessageLogging(EventId = 11075, Level = LogLevel.Information,
        Message = "GenerateDocsTranslator generated documentation for {documentedCount} member(s) in '{filePath}'")]
    public static partial IGenericMessage Generated(ILogger logger, string filePath, int documentedCount);
}
