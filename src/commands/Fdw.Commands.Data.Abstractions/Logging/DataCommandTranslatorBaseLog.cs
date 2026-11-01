using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="DataCommandTranslatorBase{TCommand}"/> construction.
/// </summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class DataCommandTranslatorBaseLog
{
    /// <summary>Traces a translator type being constructed (compile-time discovery via [TypeOption]).</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "[DataCommandTranslatorBase] Initializing translator '{translatorName}' for domain '{domainName}'")]
    public static partial IGenericMessage TranslatorInitializing(ILogger logger, string translatorName, string domainName);

    /// <summary>
    /// Logs the defect condition immediately before <see cref="DataCommandTranslatorBase{TCommand}"/>
    /// throws <see cref="System.ArgumentNullException"/> for a null or empty translator name. See the
    /// logging-pass report — the throw itself is left in place.
    /// </summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error,
        Message = "[DataCommandTranslatorBase] Translator name is required and was null or empty")]
    public static partial IGenericMessage TranslatorNameMissing(ILogger logger);
}
