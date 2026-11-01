using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.FindTypeOptionsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindTypeOptionsTranslatorLog
{
    /// <summary>Trace: TypeOption scan starting.</summary>
    [MessageLogging(EventId = 11049, Level = LogLevel.Trace,
        Message = "FindTypeOptionsTranslator scanning (collectionFilter='{collectionFilter}')")]
    public static partial IGenericMessage Scanning(ILogger logger, string collectionFilter);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11050, Level = LogLevel.Information,
        Message = "FindTypeOptionsTranslator found {count} TypeOption(s)")]
    public static partial IGenericMessage Found(ILogger logger, int count);
}
