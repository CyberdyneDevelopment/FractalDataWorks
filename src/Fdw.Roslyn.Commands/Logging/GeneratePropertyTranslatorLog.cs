using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GeneratePropertyTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GeneratePropertyTranslatorLog
{
    /// <summary>Trace: property generation starting.</summary>
    [MessageLogging(EventId = 11080, Level = LogLevel.Trace,
        Message = "GeneratePropertyTranslator generating property '{propertyName}' of type '{propertyType}' at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Generating(ILogger logger, string propertyName, string propertyType, string filePath, int line, int column);

    /// <summary>Error: PropertyName was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.PropertyNameRequired</c> (21010).</remarks>
    [MessageLogging(EventId = 21010, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: PropertyName is required")]
    public static partial IGenericMessage PropertyNameRequired(ILogger logger);

    /// <summary>Error: PropertyType was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.PropertyTypeRequired</c> (21011).</remarks>
    [MessageLogging(EventId = 21011, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: PropertyType is required")]
    public static partial IGenericMessage PropertyTypeRequired(ILogger logger);

    /// <summary>Error: neither HasGetter nor HasSetter was requested.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.PropertyMustHaveGetterOrSetter</c> (21015).</remarks>
    [MessageLogging(EventId = 21015, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: property must have a getter or a setter")]
    public static partial IGenericMessage PropertyMustHaveGetterOrSetter(ILogger logger);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no type declaration was found at the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypeDeclarationFoundAtPosition</c> (31013).</remarks>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error,
        Message = "GeneratePropertyTranslator: no type declaration found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoTypeDeclarationFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the property was generated.</summary>
    [MessageLogging(EventId = 11081, Level = LogLevel.Information,
        Message = "GeneratePropertyTranslator generated property '{propertyName}' of type '{propertyType}'")]
    public static partial IGenericMessage Generated(ILogger logger, string propertyName, string propertyType);
}
