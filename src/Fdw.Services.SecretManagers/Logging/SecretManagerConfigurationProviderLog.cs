using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// MessageLogging for SecretManagerConfigurationProvider operations.
/// EventId range: 4240-4259
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class SecretManagerConfigurationProviderLog
{
    /// <summary>
    /// Logs that a typed cache was registered for a secret manager service option type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="serviceOptionType">The service option type the typed cache was registered for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Registered typed cache for secret manager ServiceOptionType '{serviceOptionType}'")]
    public static partial IGenericMessage TypedCacheRegistered(ILogger logger, string serviceOptionType);

    /// <summary>
    /// Logs that a typed configuration lookup has started for a secret manager.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager whose typed configuration is being looked up.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "Looking up typed configuration for secret manager '{name}'")]
    public static partial IGenericMessage TypedLookupStarted(ILogger logger, string name);

    /// <summary>
    /// Logs that a typed configuration was resolved for a secret manager via its service option type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager whose typed configuration was resolved.</param>
    /// <param name="serviceOptionType">The service option type the typed configuration was resolved through.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "Typed configuration resolved for secret manager '{name}' via ServiceOptionType '{serviceOptionType}'")]
    public static partial IGenericMessage TypedLookupResolved(ILogger logger, string name, string serviceOptionType);

    /// <summary>
    /// Logs that no typed cache was registered for a secret manager service option type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="serviceOptionType">The service option type that has no registered typed cache.</param>
    /// <param name="name">The name of the secret manager whose typed cache was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Warning,
        Message = "No typed cache registered for secret manager ServiceOptionType '{serviceOptionType}' (secret manager '{name}')")]
    public static partial IGenericMessage TypedCacheNotFound(ILogger logger, string serviceOptionType, string name);

    /// <summary>
    /// Logs that a parent secret manager has no service option type, so its typed configuration cannot be resolved.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the parent secret manager that has no service option type.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Warning,
        Message = "Parent secret manager '{name}' has no ServiceOptionType -- cannot resolve typed configuration")]
    public static partial IGenericMessage NoServiceOptionType(ILogger logger, string name);

    /// <summary>
    /// Logs that no typed secret manager provider is registered for a service option type.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="serviceOptionType">The service option type that has no registered typed provider.</param>
    /// <param name="name">The name of the secret manager whose typed provider was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "No typed secret manager provider registered for service option type '{serviceOptionType}' (secret manager '{name}')")]
    public static partial IGenericMessage NoTypedProviderForServiceOptionType(ILogger logger, string serviceOptionType, string name);

    /// <summary>
    /// Logs that loading the typed secret manager body failed for a secret manager.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that caused the typed body load to fail.</param>
    /// <param name="name">The name of the secret manager whose typed body failed to load.</param>
    /// <param name="serviceOptionType">The service option type of the typed body that failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error,
        Message = "Failed to load typed secret manager body for '{name}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoadFailed(ILogger logger, Exception exception, string name, string serviceOptionType);

    /// <summary>
    /// Logs that the typed secret manager body was loaded for a secret manager.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the secret manager whose typed body was loaded.</param>
    /// <param name="serviceOptionType">The service option type of the typed body that was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Typed secret manager body loaded for '{name}' (service option type '{serviceOptionType}')")]
    public static partial IGenericMessage TypedBodyLoaded(ILogger logger, string name, string serviceOptionType);
}
