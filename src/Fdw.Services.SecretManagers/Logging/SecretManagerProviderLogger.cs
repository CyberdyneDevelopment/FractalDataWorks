using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for secret manager provider operations.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class SecretManagerProviderLogger
{
    /// <summary>
    /// Logs when getting a secret manager by configuration name.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Getting secret manager for configuration name: {configurationName}")]
    public static partial IGenericMessage GettingSecretManagerByConfigurationName(ILogger logger, string configurationName);

    /// <summary>
    /// Logs when a secret manager configuration is retrieved from cache.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Secret manager configuration retrieved from cache: {configurationName}")]
    public static partial IGenericMessage SecretManagerConfigurationRetrievedFromCache(ILogger logger, string configurationName);

    /// <summary>
    /// Logs when creating a secret manager with a specific factory.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "Creating secret manager with factory: {configurationName}, {factoryTypeName}")]
    public static partial IGenericMessage CreatingSecretManagerWithFactory(ILogger logger, string configurationName, string factoryTypeName);

    /// <summary>
    /// Logs when a secret manager configuration is loaded.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "Secret manager configuration loaded: {configurationName}, type: {secretManagerType}")]
    public static partial IGenericMessage SecretManagerConfigurationLoaded(ILogger logger, string configurationName, string secretManagerType);

    /// <summary>
    /// Logs an error when a secret manager configuration is not found.
    /// </summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Error, Message = "Secret manager configuration not found: {configurationName}")]
    public static partial IGenericMessage SecretManagerConfigurationNotFound(ILogger logger, string configurationName);

    /// <summary>
    /// Logs an error when a configuration section is not found.
    /// </summary>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error, Message = "Configuration section not found for secret manager: {configurationName}")]
    public static partial IGenericMessage ConfigurationSectionNotFound(ILogger logger, string configurationName);

    /// <summary>
    /// Logs an error when the secret manager type is not specified in configuration.
    /// </summary>
    [MessageLogging(EventId = 61005, Level = LogLevel.Error, Message = "Secret manager type not specified in configuration: {configurationName}")]
    public static partial IGenericMessage SecretManagerTypeNotSpecified(ILogger logger, string configurationName);

    /// <summary>
    /// Logs an error for an unknown secret manager type in configuration.
    /// </summary>
    [MessageLogging(EventId = 61006, Level = LogLevel.Error, Message = "Unknown secret manager type in configuration: {secretManagerType}")]
    public static partial IGenericMessage UnknownSecretManagerTypeInConfiguration(ILogger logger, string secretManagerType);

    /// <summary>
    /// Logs an error when configuration binding fails.
    /// </summary>
    [MessageLogging(EventId = 61007, Level = LogLevel.Error, Message = "Failed to bind configuration for secret manager type: {configurationTypeName}")]
    public static partial IGenericMessage ConfigurationBindingFailed(ILogger logger, string configurationTypeName);

    /// <summary>
    /// Logs an error when no factory is registered for a secret manager type.
    /// </summary>
    [MessageLogging(EventId = 61008, Level = LogLevel.Error, Message = "No factory registered for secret manager type: {secretManagerType}")]
    public static partial IGenericMessage NoFactoryRegistered(ILogger logger, string secretManagerType);

    /// <summary>
    /// Logs an error when the factory is not registered in DI.
    /// </summary>
    [MessageLogging(EventId = 61009, Level = LogLevel.Error, Message = "Factory not registered in DI for secret manager '{configurationName}' with type '{secretManagerType}'")]
    public static partial IGenericMessage FactoryNotRegisteredInDi(ILogger logger, string configurationName, string secretManagerType);

    /// <summary>
    /// Logs when getting a secret manager by type.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "Getting secret manager for secret manager type: {secretManagerType}")]
    public static partial IGenericMessage GettingSecretManager(ILogger logger, string secretManagerType);

    /// <summary>
    /// Logs when subscribed to configuration changes.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Subscribed to secret manager configuration changes")]
    public static partial IGenericMessage SubscribedToConfigurationChanges(ILogger logger);

    /// <summary>
    /// Logs when configuration has changed and cache is being cleared.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information, Message = "Secret manager configuration changed, clearing cache")]
    public static partial IGenericMessage ConfigurationChanged(ILogger logger);

    /// <summary>
    /// Logs when the cache is cleared.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "Secret manager cache cleared, {count} entries removed")]
    public static partial IGenericMessage CacheCleared(ILogger logger, int count);

    /// <summary>
    /// Logs an exception when getting a secret manager by name.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Exception while getting secret manager by name '{configurationName}': {errorMessage}")]
    public static partial IGenericMessage GetSecretManagerByNameException(ILogger logger, string configurationName, string errorMessage);

    /// <summary>
    /// Logs an error when configuration loading fails.
    /// </summary>
    [MessageLogging(EventId = 61010, Level = LogLevel.Error, Message = "Configuration load failed for '{configurationName}' with type '{typeName}'")]
    public static partial IGenericMessage ConfigurationLoadFailed(ILogger logger, string configurationName, string typeName);
}
