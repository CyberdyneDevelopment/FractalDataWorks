using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Conventions.Translators.FindServiceTypesTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class FindServiceTypesTranslatorLog
{
    /// <summary>Trace: ServiceType scan starting.</summary>
    [MessageLogging(EventId = 11045, Level = LogLevel.Trace,
        Message = "FindServiceTypesTranslator scanning the solution for ServiceType attributes")]
    public static partial IGenericMessage Scanning(ILogger logger);

    /// <summary>Information: the scan completed.</summary>
    [MessageLogging(EventId = 11046, Level = LogLevel.Information,
        Message = "FindServiceTypesTranslator found {count} ServiceType(s)")]
    public static partial IGenericMessage Found(ILogger logger, int count);
}
