using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for ConnectionConfigurationProvider operations.
/// EventId range: 4200-4219
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionConfigurationProviderLog
{
    /// <summary>
    /// Logs that a typed connection provider was registered for a given service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type the typed connection provider was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Registering typed connection provider for service option type '{serviceOptionType}'")]
    public static partial IGenericMessage TypedProviderRegistered(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs that the typed connection body is being loaded for a connection using a service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the connection whose typed body is being loaded.</param>
    /// <param name="serviceOptionType">The service option type used to load the typed connection body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Loading typed connection body for connection '{name}' using service option type '{serviceOptionType}'")]
    public static partial IGenericMessage LoadingTypedBody(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs that no typed connection provider is registered for the requested service option type.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="serviceOptionType">The service option type for which no typed connection provider was found.</param>
    /// <param name="name">The name of the connection that could not be loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error,
        Message = "No typed connection provider registered for service option type '{serviceOptionType}' (connection '{name}')")]
    public static partial IGenericMessage NoTypedProviderForServiceOptionType(ILogger logger, string serviceOptionType, string name);

    /// <summary>
    /// Logs that loading the typed connection body failed for a connection.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="name">The name of the connection whose typed body failed to load.</param>
    /// <param name="serviceOptionType">The service option type used when the load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to load typed connection body for connection '{name}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, Exception exception, string name, string serviceOptionType);

    /// <summary>
    /// Logs that a connection header has no ServiceOptionType, so its typed body cannot be loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the connection header missing a ServiceOptionType.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "Connection header '{name}' has no ServiceOptionType — typed body cannot be loaded")]
    public static partial IGenericMessage MissingServiceOptionType(ILogger logger, string name);

    /// <summary>
    /// Logs that the typed connection body was successfully loaded for a connection.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the connection whose typed body was loaded.</param>
    /// <param name="serviceOptionType">The service option type used to load the typed connection body.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Typed connection body loaded for connection '{name}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string name, string serviceOptionType);
}
