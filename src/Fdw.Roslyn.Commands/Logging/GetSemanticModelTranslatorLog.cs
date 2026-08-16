using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.GetSemanticModelTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetSemanticModelTranslatorLog
{
    /// <summary>Trace: semantic model retrieval starting.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Trace,
        Message = "GetSemanticModelTranslator retrieving semantic model for '{filePath}'")]
    public static partial IGenericMessage Retrieving(ILogger logger, string filePath);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetSemanticModelTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetSemanticModelTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's semantic model or syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSemanticModel</c> (91005).</remarks>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "GetSemanticModelTranslator: failed to get semantic model for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSemanticModel(ILogger logger, string filePath);

    /// <summary>Information: semantic model retrieval completed.</summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Information,
        Message = "GetSemanticModelTranslator retrieved semantic model for '{filePath}': {symbolCount} declared symbol(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, string filePath, int symbolCount);
}
