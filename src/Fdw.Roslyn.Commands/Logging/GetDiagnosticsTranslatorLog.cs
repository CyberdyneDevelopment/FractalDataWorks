using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Analysis.Translators.GetDiagnosticsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GetDiagnosticsTranslatorLog
{
    /// <summary>Trace: diagnostics retrieval starting.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Trace,
        Message = "GetDiagnosticsTranslator retrieving diagnostics (filePath='{filePath}', projectName='{projectName}', minSeverity={minSeverity})")]
    public static partial IGenericMessage Retrieving(ILogger logger, string filePath, string projectName, string minSeverity);

    /// <summary>Warning: the requested severity string did not parse, so Warning was used instead.</summary>
    [MessageLogging(EventId = 21100, Level = LogLevel.Warning,
        Message = "GetDiagnosticsTranslator: severity '{requestedSeverity}' did not parse, falling back to Warning")]
    public static partial IGenericMessage InvalidSeverityFallback(ILogger logger, string requestedSeverity);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GetDiagnosticsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GetDiagnosticsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Information: diagnostics retrieval completed.</summary>
    [MessageLogging(EventId = 11016, Level = LogLevel.Information,
        Message = "GetDiagnosticsTranslator found {count} diagnostic(s)")]
    public static partial IGenericMessage Retrieved(ILogger logger, int count);
}
