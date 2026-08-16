using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Formatting.Translators.SortMembersTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class SortMembersTranslatorLog
{
    /// <summary>Trace: member sort starting.</summary>
    [MessageLogging(EventId = 11068, Level = LogLevel.Trace,
        Message = "SortMembersTranslator sorting members in '{filePath}' (line={line})")]
    public static partial IGenericMessage Sorting(ILogger logger, string filePath, int line);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "SortMembersTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "SortMembersTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "SortMembersTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no type declaration was found at the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypeDeclarationFoundAtPosition</c> (31013).</remarks>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error,
        Message = "SortMembersTranslator: no type declaration found at line {line} in '{filePath}'")]
    public static partial IGenericMessage NoTypeDeclarationFoundAtPosition(ILogger logger, string filePath, int line);

    /// <summary>Information: sorting completed.</summary>
    [MessageLogging(EventId = 11069, Level = LogLevel.Information,
        Message = "SortMembersTranslator sorted {memberCount} member(s) across {changedTypeCount} changed type(s) in '{filePath}'")]
    public static partial IGenericMessage Sorted(ILogger logger, string filePath, int memberCount, int changedTypeCount);
}
