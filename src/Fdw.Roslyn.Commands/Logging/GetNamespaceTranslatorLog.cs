using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Navigation.Translators.GetNamespaceTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetNamespaceTranslatorLog
{
    /// <summary>Trace: namespace lookup starting.</summary>
    [MessageLogging(EventId = 11098, Level = LogLevel.Trace,
        Message = "GetNamespaceTranslator finding namespace at '{filePath}' {line}:{column}")]
    public static partial IGenericMessage Finding(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetNamespaceTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetNamespaceTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the document's syntax root could not be retrieved.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetSyntaxRoot</c> (91006).</remarks>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error,
        Message = "GetNamespaceTranslator: failed to get syntax root for '{filePath}'")]
    public static partial IGenericMessage FailedToGetSyntaxRoot(ILogger logger, string filePath);

    /// <summary>Error: no namespace declaration was found at or above the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoNamespaceFoundAtPosition</c> (31005).</remarks>
    [MessageLogging(EventId = 31005, Level = LogLevel.Error,
        Message = "GetNamespaceTranslator: no namespace found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoNamespaceFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Information: the lookup completed.</summary>
    [MessageLogging(EventId = 11099, Level = LogLevel.Information,
        Message = "GetNamespaceTranslator found namespace '{namespaceName}'")]
    public static partial IGenericMessage Found(ILogger logger, string namespaceName);
}
