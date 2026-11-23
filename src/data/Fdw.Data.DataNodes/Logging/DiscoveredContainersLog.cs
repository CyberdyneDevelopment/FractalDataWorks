using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for building the containers a schema discovery returns (FDW-6309). EventId range: 61046.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DiscoveredContainersLog
{
    /// <summary>
    /// Logs that a key type the discovered keys are built with is not registered, so the discovery cannot state its keys.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyType">The name of the key type that was looked up.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61046, Level = LogLevel.Error,
        Message = "[DiscoveredContainers] Key type '{keyType}' is not registered, so the keys of a schema discovery cannot be built")]
    public static partial IGenericMessage KeyTypeNotRegistered(ILogger logger, string keyType);
}
