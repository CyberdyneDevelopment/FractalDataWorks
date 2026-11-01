using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.AddUsingsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AddUsingsTranslatorLog
{
    /// <summary>Trace: missing-using resolution starting.</summary>
    [MessageLogging(EventId = 11122, Level = LogLevel.Trace,
        Message = "AddUsingsTranslator resolving missing usings for '{filePath}'")]
    public static partial IGenericMessage Adding(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "AddUsingsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "AddUsingsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "AddUsingsTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Information: missing usings were added.</summary>
    [MessageLogging(EventId = 11123, Level = LogLevel.Information,
        Message = "AddUsingsTranslator added {usingCount} using directive(s) to '{filePath}'")]
    public static partial IGenericMessage Added(ILogger logger, string filePath, int usingCount);
}
