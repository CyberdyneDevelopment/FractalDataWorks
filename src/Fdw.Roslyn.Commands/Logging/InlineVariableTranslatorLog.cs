using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.InlineVariableTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class InlineVariableTranslatorLog
{
    /// <summary>Trace: variable inlining starting.</summary>
    [MessageLogging(EventId = 11130, Level = LogLevel.Trace,
        Message = "InlineVariableTranslator inlining variable at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Inlining(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "InlineVariableTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "InlineVariableTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "InlineVariableTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: the symbol at the given position is not a local variable.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.SymbolNotLocalVariable</c> (21019).</remarks>
    [MessageLogging(EventId = 21019, Level = LogLevel.Error,
        Message = "InlineVariableTranslator: symbol at {line}:{column} in '{filePath}' is not a local variable")]
    public static partial IGenericMessage SymbolNotLocalVariable(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target variable has no initializer to inline.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.VariableMustHaveInitializerToBeInlined</c> (21022).</remarks>
    [MessageLogging(EventId = 21022, Level = LogLevel.Error,
        Message = "InlineVariableTranslator: variable '{variableName}' in '{filePath}' has no initializer to inline")]
    public static partial IGenericMessage VariableMustHaveInitializerToBeInlined(ILogger logger, string filePath, string variableName);

    /// <summary>Information: the variable was inlined.</summary>
    [MessageLogging(EventId = 11131, Level = LogLevel.Information,
        Message = "InlineVariableTranslator inlined variable '{variableName}' at {referenceCount} location(s)")]
    public static partial IGenericMessage Inlined(ILogger logger, string variableName, int referenceCount);
}
