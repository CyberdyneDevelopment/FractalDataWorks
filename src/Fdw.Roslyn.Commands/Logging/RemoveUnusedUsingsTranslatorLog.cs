using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.RemoveUnusedUsingsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RemoveUnusedUsingsTranslatorLog
{
    /// <summary>Trace: unused-using scan starting.</summary>
    [MessageLogging(EventId = 11138, Level = LogLevel.Trace,
        Message = "RemoveUnusedUsingsTranslator scanning '{filePath}'")]
    public static partial IGenericMessage Scanning(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "RemoveUnusedUsingsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "RemoveUnusedUsingsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "RemoveUnusedUsingsTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Debug: no unused using directives were found, so nothing changed.</summary>
    [MessageLogging(EventId = 11139, Level = LogLevel.Debug,
        Message = "RemoveUnusedUsingsTranslator: '{filePath}' has no unused using directives")]
    public static partial IGenericMessage NoneFound(ILogger logger, string filePath);

    /// <summary>Information: unused usings were removed.</summary>
    [MessageLogging(EventId = 11140, Level = LogLevel.Information,
        Message = "RemoveUnusedUsingsTranslator removed {removedCount} unused using directive(s) from '{filePath}'")]
    public static partial IGenericMessage Removed(ILogger logger, string filePath, int removedCount);
}
