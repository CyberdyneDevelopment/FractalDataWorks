using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.FindTypeCollectionsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindTypeCollectionsTranslatorLog
{
    /// <summary>Trace: TypeCollection scan starting.</summary>
    [MessageLogging(EventId = 11047, Level = LogLevel.Trace,
        Message = "FindTypeCollectionsTranslator scanning the solution for TypeCollection attributes")]
    public static partial IGenericMessage Scanning(ILogger logger);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11048, Level = LogLevel.Information,
        Message = "FindTypeCollectionsTranslator found {count} TypeCollection(s)")]
    public static partial IGenericMessage Found(ILogger logger, int count);
}
