using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Compilation.Translators.GetSyntaxTreeTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetSyntaxTreeTranslatorLog
{
    /// <summary>Trace: syntax tree retrieval starting.</summary>
    [MessageLogging(EventId = 11035, Level = LogLevel.Trace,
        Message = "GetSyntaxTreeTranslator retrieving syntax tree for '{filePath}' (includeTrivia={includeTrivia})")]
    public static partial IGenericMessage Retrieving(ILogger logger, string filePath, bool includeTrivia);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetSyntaxTreeTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetSyntaxTreeTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax tree could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxTree</c> (91007).</remarks>
    [MessageLogging(EventId = 91007, Level = LogLevel.Error,
        Message = "GetSyntaxTreeTranslator: failed to get syntax tree for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxTree(ILogger logger, string filePath);

    /// <summary>Information: syntax tree retrieval completed.</summary>
    [MessageLogging(EventId = 11036, Level = LogLevel.Information,
        Message = "GetSyntaxTreeTranslator retrieved syntax tree for '{filePath}': {nodeCount} node(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, string filePath, int nodeCount);
}
