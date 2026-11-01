using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.MoveToFileTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class MoveToFileTranslatorLog
{
    /// <summary>Trace: move-to-file starting.</summary>
    [MessageLogging(EventId = 11134, Level = LogLevel.Trace,
        Message = "MoveToFileTranslator moving type at '{filePath}' {line}:{column} to its own file")]
    public static partial IGenericMessage Moving(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "MoveToFileTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "MoveToFileTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "MoveToFileTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no type declaration was found at the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypeDeclarationFoundAtPosition</c> (31013).</remarks>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error,
        Message = "MoveToFileTranslator: no type declaration found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoTypeDeclarationFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the type declaration resolved but its symbol could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetTypeSymbol</c> (91008).</remarks>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "MoveToFileTranslator: failed to get type symbol at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage FailedToGetTypeSymbol(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target type is already the only type in the file.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.TypeAlreadyOnlyTypeInFile</c> (40001).</remarks>
    [MessageLogging(EventId = 40001, Level = LogLevel.Error,
        Message = "MoveToFileTranslator: type '{typeName}' is already the only type in '{filePath}'")]
    public static partial IGenericMessage TypeAlreadyOnlyTypeInFile(ILogger logger, string filePath, string typeName);

    /// <summary>Information: the type was moved to its own file.</summary>
    [MessageLogging(EventId = 11135, Level = LogLevel.Information,
        Message = "MoveToFileTranslator moved type '{typeName}' to '{newFileName}'")]
    public static partial IGenericMessage Moved(ILogger logger, string typeName, string newFileName);
}
