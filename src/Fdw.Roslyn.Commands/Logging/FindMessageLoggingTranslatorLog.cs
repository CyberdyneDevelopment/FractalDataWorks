using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.FindMessageLoggingTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindMessageLoggingTranslatorLog
{
    /// <summary>Trace: MessageLogging attribute scan starting.</summary>
    [MessageLogging(EventId = 11041, Level = LogLevel.Trace,
        Message = "FindMessageLoggingTranslator scanning (projectFilter='{projectFilter}')")]
    public static partial IGenericMessage Scanning(ILogger logger, string projectFilter);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11042, Level = LogLevel.Information,
        Message = "FindMessageLoggingTranslator found {count} MessageLogging method(s)")]
    public static partial IGenericMessage Found(ILogger logger, int count);
}
