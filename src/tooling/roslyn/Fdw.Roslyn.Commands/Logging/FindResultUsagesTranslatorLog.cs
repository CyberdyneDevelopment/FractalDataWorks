using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.FindResultUsagesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindResultUsagesTranslatorLog
{
    /// <summary>Trace: IGenericResult usage scan starting.</summary>
    [MessageLogging(EventId = 11043, Level = LogLevel.Trace,
        Message = "FindResultUsagesTranslator scanning (projectFilter='{projectFilter}')")]
    public static partial IGenericMessage Scanning(ILogger logger, string projectFilter);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11044, Level = LogLevel.Information,
        Message = "FindResultUsagesTranslator found {count} method(s) returning IGenericResult")]
    public static partial IGenericMessage Found(ILogger logger, int count);
}
