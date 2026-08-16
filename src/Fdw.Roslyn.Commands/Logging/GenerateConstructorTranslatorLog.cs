using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GenerateConstructorTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GenerateConstructorTranslatorLog
{
    /// <summary>Trace: constructor generation starting.</summary>
    [MessageLogging(EventId = 11072, Level = LogLevel.Trace,
        Message = "GenerateConstructorTranslator generating constructor at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Generating(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GenerateConstructorTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GenerateConstructorTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GenerateConstructorTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no type declaration was found at the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypeDeclarationFoundAtPosition</c> (31013).</remarks>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error,
        Message = "GenerateConstructorTranslator: no type declaration found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoTypeDeclarationFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the type declaration resolved but its symbol could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetTypeSymbol</c> (91008).</remarks>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "GenerateConstructorTranslator: failed to get type symbol at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage FailedToGetTypeSymbol(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target type has no fields eligible for constructor parameters.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoFieldsFoundToGenerateConstructorParameters</c> (31004).</remarks>
    [MessageLogging(EventId = 31004, Level = LogLevel.Error,
        Message = "GenerateConstructorTranslator: type '{typeName}' has no fields eligible for constructor parameters")]
    public static partial IGenericMessage NoFieldsFoundToGenerateConstructorParameters(ILogger logger, string typeName);

    /// <summary>Information: the constructor was generated.</summary>
    [MessageLogging(EventId = 11073, Level = LogLevel.Information,
        Message = "GenerateConstructorTranslator generated constructor for '{typeName}' with {parameterCount} parameter(s)")]
    public static partial IGenericMessage Generated(ILogger logger, string typeName, int parameterCount);
}
