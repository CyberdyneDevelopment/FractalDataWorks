using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.EncapsulateFieldTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class EncapsulateFieldTranslatorLog
{
    /// <summary>Trace: field encapsulation starting.</summary>
    [MessageLogging(EventId = 11124, Level = LogLevel.Trace,
        Message = "EncapsulateFieldTranslator encapsulating field at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Encapsulating(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "EncapsulateFieldTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "EncapsulateFieldTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "EncapsulateFieldTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a field.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolNotField</c> (21018).</remarks>
    [MessageLogging(EventId = 21018, Level = LogLevel.Error,
        Message = "EncapsulateFieldTranslator: symbol at {line}:{column} in '{filePath}' is not a field")]
    public static partial IGenericMessage SymbolNotField(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the renamed field declaration could not be located.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.CouldNotFindFieldDeclaration</c> (31000).</remarks>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error,
        Message = "EncapsulateFieldTranslator: could not find renamed field declaration '{propertyName}' in '{filePath}'")]
    public static partial IGenericMessage CouldNotFindFieldDeclaration(ILogger logger, string filePath, string propertyName);

    /// <summary>Information: the field was encapsulated.</summary>
    [MessageLogging(EventId = 11125, Level = LogLevel.Information,
        Message = "EncapsulateFieldTranslator encapsulated field '{fieldName}' as property '{propertyName}' with {changeCount} change(s) across {fileCount} file(s)")]
    public static partial IGenericMessage Encapsulated(ILogger logger, string fieldName, string propertyName, int changeCount, int fileCount);
}
