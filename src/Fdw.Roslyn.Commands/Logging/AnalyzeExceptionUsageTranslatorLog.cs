using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.AnalyzeExceptionUsageTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class AnalyzeExceptionUsageTranslatorLog
{
    /// <summary>Trace: exception-usage scan starting.</summary>
    [MessageLogging(EventId = 11039, Level = LogLevel.Trace,
        Message = "AnalyzeExceptionUsageTranslator scanning (projectFilter='{projectFilter}')")]
    public static partial IGenericMessage Analyzing(ILogger logger, string projectFilter);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11040, Level = LogLevel.Information,
        Message = "AnalyzeExceptionUsageTranslator found {throwCount} throw statement(s) and {tryCatchCount} try-catch block(s)")]
    public static partial IGenericMessage Analyzed(ILogger logger, int throwCount, int tryCatchCount);
}
