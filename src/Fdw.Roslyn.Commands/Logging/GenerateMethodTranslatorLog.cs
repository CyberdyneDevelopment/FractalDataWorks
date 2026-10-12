using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GenerateMethodTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GenerateMethodTranslatorLog
{
    /// <summary>Trace: method generation starting.</summary>
    [MessageLogging(EventId = 11078, Level = LogLevel.Trace,
        Message = "GenerateMethodTranslator generating method '{methodName}' at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Generating(ILogger logger, string methodName, string filePath, int line, int column);

    /// <summary>Error: MethodName was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.MethodNameRequired</c> (21006).</remarks>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error,
        Message = "GenerateMethodTranslator: MethodName is required")]
    public static partial IGenericMessage MethodNameRequired(ILogger logger);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GenerateMethodTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GenerateMethodTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GenerateMethodTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no type declaration was found at the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypeDeclarationFoundAtPosition</c> (31013).</remarks>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error,
        Message = "GenerateMethodTranslator: no type declaration found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoTypeDeclarationFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the method was generated.</summary>
    [MessageLogging(EventId = 11079, Level = LogLevel.Information,
        Message = "GenerateMethodTranslator generated method '{methodName}' with {parameterCount} parameter(s)")]
    public static partial IGenericMessage Generated(ILogger logger, string methodName, int parameterCount);
}
