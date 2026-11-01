using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.ValidateSyntaxTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ValidateSyntaxTranslatorLog
{
    /// <summary>Trace: validation starting.</summary>
    [MessageLogging(EventId = 11037, Level = LogLevel.Trace,
        Message = "ValidateSyntaxTranslator validating (filePath='{filePath}', hasInlineCode={hasInlineCode})")]
    public static partial IGenericMessage Validating(ILogger logger, string filePath, bool hasInlineCode);

    /// <summary>Error: neither FilePath nor Code was supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.EitherFilePathOrCodeRequired</c> (21002).</remarks>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error,
        Message = "ValidateSyntaxTranslator: either FilePath or Code is required")]
    public static partial IGenericMessage EitherFilePathOrCodeRequired(ILogger logger);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "ValidateSyntaxTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "ValidateSyntaxTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax tree could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxTree</c> (91007).</remarks>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error,
        Message = "ValidateSyntaxTranslator: failed to get syntax tree for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxTree(ILogger logger, string filePath);

    /// <summary>Information: validation completed.</summary>
    [MessageLogging(EventId = 11038, Level = LogLevel.Information,
        Message = "ValidateSyntaxTranslator validated: isValid={isValid}, {errorCount} error(s)")]
    public static partial IGenericMessage Validated(ILogger logger, bool isValid, int errorCount);
}
