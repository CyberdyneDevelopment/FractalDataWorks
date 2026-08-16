using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.ExtractMethodTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ExtractMethodTranslatorLog
{
    /// <summary>Trace: method extraction starting.</summary>
    [MessageLogging(EventId = 11128, Level = LogLevel.Trace,
        Message = "ExtractMethodTranslator extracting method '{methodName}' from '{filePath}' {startLine}:{startColumn}-{endLine}:{endColumn}")]
    public static partial IGenericMessage Extracting(ILogger logger, string filePath, string methodName, int startLine, int startColumn, int endLine, int endColumn);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no statements were found within the selected range.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoStatementsFoundInSelectedRange</c> (31009).</remarks>
    [MessageLogging(EventId = 31009, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: no statements found in the selected range of '{filePath}'")]
    public static partial IGenericMessage NoStatementsFoundInSelectedRange(ILogger logger, string filePath);

    /// <summary>Error: the selected code does not sit within a method.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SelectedCodeNotWithinMethod</c> (21016).</remarks>
    [MessageLogging(EventId = 21016, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: selected code in '{filePath}' is not within a method")]
    public static partial IGenericMessage SelectedCodeNotWithinMethod(ILogger logger, string filePath);

    /// <summary>Error: data-flow analysis over the selected statements did not succeed.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDataFlow</c> (91002).</remarks>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: failed to analyze data flow in '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDataFlow(ILogger logger, string filePath);

    /// <summary>Error: the containing type declaration could not be located.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.CouldNotFindContainingType</c> (30000).</remarks>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "ExtractMethodTranslator: could not find containing type for method '{containingMethodName}' in '{filePath}'")]
    public static partial IGenericMessage CouldNotFindContainingType(ILogger logger, string filePath, string containingMethodName);

    /// <summary>Information: the method was extracted.</summary>
    [MessageLogging(EventId = 11129, Level = LogLevel.Information,
        Message = "ExtractMethodTranslator extracted method '{methodName}' from '{containingMethodName}' with {statementCount} statement(s)")]
    public static partial IGenericMessage Extracted(ILogger logger, string methodName, string containingMethodName, int statementCount);
}
