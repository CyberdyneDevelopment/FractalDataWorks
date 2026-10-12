using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging methods for data command operations.
/// EventId range: 2001-2050.
/// </summary>
/// <remarks>
/// Note: EventId 2001 is reserved for MsSql Translator in MsSqlTranslatorLog.
/// </remarks>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class DataCommandLog
{
    /// <summary>
    /// Logs that a data command translator was registered.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "Registered data command translator: {translatorName} ({translatorType})")]
    public static partial IGenericMessage TranslatorRegistered(ILogger logger, string translatorName, string translatorType);
}
