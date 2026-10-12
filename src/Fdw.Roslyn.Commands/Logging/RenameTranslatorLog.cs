using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.RenameTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class RenameTranslatorLog
{
    /// <summary>Trace: rename starting.</summary>
    [MessageLogging(EventId = 11141, Level = LogLevel.Trace,
        Message = "RenameTranslator renaming symbol at '{filePath}' {line}:{column} to '{newName}'")]
    public static partial IGenericMessage Renaming(ILogger logger, string filePath, int line, int column, string newName);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "RenameTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "RenameTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "RenameTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no symbol was found at the given position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoSymbolFoundAtPosition</c> (31012).</remarks>
    [MessageLogging(EventId = 31012, Level = LogLevel.Error,
        Message = "RenameTranslator: no symbol found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoSymbolFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the rename completed.</summary>
    [MessageLogging(EventId = 11142, Level = LogLevel.Information,
        Message = "RenameTranslator renamed '{oldName}' to '{newName}' with {changeCount} change(s) across {fileCount} file(s)")]
    public static partial IGenericMessage Renamed(ILogger logger, string oldName, string newName, int changeCount, int fileCount);
}
