using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Abstractions.Logging;

/// <summary>
/// MessageLogging for CalculationEntityTypeBase contract-violation guards.
/// EventId range: 4067-4068 (within the shared 4058-4099 block).
/// </summary>
[MessageLoggingTypeCode("CALC")]
internal static partial class CalculationEntityTypeBaseLog
{
    /// <summary>
    /// Logs that the entity type declares no typed configuration, so LoadTypedConfiguration must not be called for it.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="typeName">The name of the entity type that declares no typed configuration.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "Entity type '{typeName}' declares no typed configuration (TypedContainerName is null) — LoadTypedConfiguration must not be called for this type")]
    public static partial IGenericMessage LoadTypedConfigurationNotSupported(ILogger logger, string typeName);

    /// <summary>
    /// Logs that the entity type declares no typed configuration, so SaveTypedConfiguration must not be called for it.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="typeName">The name of the entity type that declares no typed configuration.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "Entity type '{typeName}' declares no typed configuration (TypedContainerName is null) — SaveTypedConfiguration must not be called for this type")]
    public static partial IGenericMessage SaveTypedConfigurationNotSupported(ILogger logger, string typeName);
}
