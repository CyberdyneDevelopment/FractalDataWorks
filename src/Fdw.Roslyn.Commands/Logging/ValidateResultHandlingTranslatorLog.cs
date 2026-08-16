using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.ValidateResultHandlingTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class ValidateResultHandlingTranslatorLog
{
    /// <summary>Trace: the audit is starting.</summary>
    [MessageLogging(EventId = 11051, Level = LogLevel.Trace,
        Message = "ValidateResultHandlingTranslator auditing (projectFilter='{projectFilter}')")]
    public static partial IGenericMessage Scanning(ILogger logger, string projectFilter);

    /// <summary>Warning: a single project's scan failed and was skipped (best-effort scan).</summary>
    [MessageLogging(EventId = 71100, Level = LogLevel.Warning,
        Message = "ValidateResultHandlingTranslator: scan of project '{projectName}' failed and was skipped: {exceptionType} — {exceptionMessage}")]
    public static partial IGenericMessage ProjectScanFailed(ILogger logger, string projectName, string exceptionType, string exceptionMessage);

    /// <summary>Information: the audit completed.</summary>
    [MessageLogging(EventId = 11052, Level = LogLevel.Information,
        Message = "ValidateResultHandlingTranslator found {issueCount} discarded IGenericResult call site(s)")]
    public static partial IGenericMessage Found(ILogger logger, int issueCount);
}
