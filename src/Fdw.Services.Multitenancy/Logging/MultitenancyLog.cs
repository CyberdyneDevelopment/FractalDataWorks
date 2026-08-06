using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Multitenancy.Logging;

/// <summary>
/// MessageLogging for the multitenancy domain (configuration lookup, option registration).
/// </summary>
[MessageLoggingTypeCode("MULTITENANCY")]
public static partial class MultitenancyLog
{
    /// <summary>
    /// Logs when <c>ConfigurationSchema.Multitenancy</c> is null/whitespace — the host's
    /// configurationSchema.json must declare which Multitenancy option it runs; there is no
    /// "section absent" fallback (NO FALLBACKS).
    /// </summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Critical,
        Message = "[Multitenancy] ConfigurationSchema.Multitenancy is required — none was found in configurationSchema.json")]
    public static partial IGenericMessage ChoiceMissing(
        ILogger logger);

    /// <summary>
    /// Logs when <c>ConfigurationSchema.Multitenancy</c> names a ServiceOptionType that does not match
    /// any registered <see cref="MultitenancyTypes"/> option.
    /// </summary>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Critical,
        Message = "[Multitenancy] ConfigurationSchema.Multitenancy '{choice}' does not match any registered Multitenancy option")]
    public static partial IGenericMessage ChoiceNotFound(
        ILogger logger,
        string choice);
}
