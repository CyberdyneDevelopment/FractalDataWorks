using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// Static logger class for authentication provider operations.
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION")]
public static partial class AuthenticationProviderLogger
{
    /// <summary>
    /// Logs when getting an authentication service for a specific type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationType">The type of authentication being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Getting authentication service for type: {authenticationType}")]
    public static partial IGenericMessage GettingAuthentication(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when no factory is registered for an authentication type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationType">The authentication type with no factory.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61006,
        Level = LogLevel.Error,
        Message = "No factory registered for authentication type: {authenticationType}")]
    public static partial IGenericMessage NoFactoryRegistered(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when getting an authentication service by configuration name.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configurationName">The name of the configuration being used.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Getting authentication service by configuration name: {configurationName}")]
    public static partial IGenericMessage GettingAuthenticationByConfigurationName(
        ILogger logger,
        string configurationName);

    /// <summary>
    /// Logs when authentication configuration is not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationName">The name of the authentication configuration that was not found.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "Authentication configuration not found: {authenticationName}")]
    public static partial IGenericMessage AuthenticationConfigurationNotFound(
        ILogger logger,
        string authenticationName);

    /// <summary>
    /// Logs when configuration loading fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationName">The authentication name that failed to load.</param>
    /// <param name="typeName">The type name of the configuration.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "Failed to load configuration '{authenticationName}' for type '{typeName}'")]
    public static partial IGenericMessage ConfigurationLoadFailed(
        ILogger logger,
        string authenticationName,
        string typeName);

    /// <summary>
    /// Logs when an authentication configuration is successfully loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationName">The name of the authentication.</param>
    /// <param name="authenticationType">The type of authentication.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Loaded authentication configuration '{authenticationName}' (type: {authenticationType})")]
    public static partial IGenericMessage AuthenticationConfigurationLoaded(
        ILogger logger,
        string authenticationName,
        string authenticationType);

    /// <summary>
    /// Logs when an authentication factory is not registered in DI.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationName">The name of the authentication.</param>
    /// <param name="factoryType">The factory type that was not found.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61007,
        Level = LogLevel.Error,
        Message = "Factory for authentication '{authenticationName}' not registered in DI. Expected type: {factoryType}")]
    public static partial IGenericMessage FactoryNotRegisteredInDi(
        ILogger logger,
        string authenticationName,
        string factoryType);

    /// <summary>
    /// Logs when creating an authentication service using factory.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authenticationName">The name of the authentication.</param>
    /// <param name="factoryType">The factory type being used.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Creating authentication service '{authenticationName}' using factory {factoryType}")]
    public static partial IGenericMessage CreatingAuthenticationWithFactory(
        ILogger logger,
        string authenticationName,
        string factoryType);

    /// <summary>
    /// Logs when getting authentication service by name fails with exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="authenticationName">The authentication name that failed.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Error,
        Message = "Failed to get authentication service by name: {authenticationName}")]
    public static partial IGenericMessage GetAuthenticationByNameException(
        ILogger logger,
        Exception exception,
        string authenticationName);

    /// <summary>
    /// Logs when attempting to get a typed authentication service.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetType">The target type being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Attempting to get authentication service as type: {targetType}")]
    public static partial IGenericMessage AttemptingTypedAuthentication(
        ILogger logger,
        string targetType);

    /// <summary>
    /// Logs when authentication service cast to specific type succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetType">The target type that was successfully cast to.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Debug,
        Message = "Successfully cast authentication service to type: {targetType}")]
    public static partial IGenericMessage AuthenticationCastSucceeded(
        ILogger logger,
        string targetType);

    /// <summary>
    /// Logs when authentication service cast to specific type fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="targetType">The target type that failed to cast to.</param>
    /// <param name="actualType">The actual type of the authentication service.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 91007,
        Level = LogLevel.Warning,
        Message = "Failed to cast authentication service to type: {targetType}. Actual type: {actualType}")]
    public static partial IGenericMessage AuthenticationCastFailed(
        ILogger logger,
        string targetType,
        string actualType);

    /// <summary>
    /// Logs when the authentication cache is cleared.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of cache entries cleared.</param>
    [LoggerMessage(
        EventId = 7214,
        Level = LogLevel.Debug,
        Message = "Cleared {count} cached authentication configuration(s)")]
    public static partial void CacheCleared(ILogger logger, int count);

    /// <summary>
    /// Logs when parent provider registration fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="error">The error message from the failed result.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61008,
        Level = LogLevel.Error,
        Message = "Failed to register parent configuration provider: {error}")]
    public static partial IGenericMessage DomainConfigurationProviderRegistrationFailed(
        ILogger logger,
        string error);
}
