using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// Static logger class for authentication provider operations.
/// </summary>
[MessageLoggingTypeCode("AUTHENTICATION")]
public static partial class AuthenticationLogger
{
    /// <summary>
    /// Logs when getting an authentication service for a specific type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="authenticationType">The type of authentication service being requested.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Getting authentication service for type: {authenticationType}")]
    public static partial IGenericMessage GettingAuthenticationService(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when an unknown authentication type is encountered.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="authenticationType">The unknown authentication type.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Unknown authentication type: {authenticationType}")]
    public static partial IGenericMessage UnknownAuthenticationType(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when no factory is registered for an authentication type.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="authenticationType">The authentication type with no factory.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Error,
        Message = "No factory registered for authentication type: {authenticationType}")]
    public static partial IGenericMessage NoFactoryRegistered(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when an authentication service is successfully created.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="authenticationType">The type of authentication service that was created.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Successfully created authentication service for type: {authenticationType}")]
    public static partial IGenericMessage AuthenticationServiceCreated(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when authentication service creation fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="authenticationType">The authentication type that failed to create.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Failed to create authentication service for type: {authenticationType}. Error: {error}")]
    public static partial IGenericMessage AuthenticationServiceCreationFailed(
        ILogger logger,
        string authenticationType,
        string error);

    /// <summary>
    /// Logs when authentication service creation throws an exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="authenticationType">The authentication type that failed to create.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Failed to create authentication service for type {authenticationType}")]
    public static partial IGenericMessage AuthenticationServiceCreationException(
        ILogger logger,
        Exception exception,
        string authenticationType);

    /// <summary>
    /// Logs when getting an authentication service by configuration name.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="configurationName">The name of the configuration being used.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Getting authentication service by configuration name: {configurationName}")]
    public static partial IGenericMessage GettingAuthenticationServiceByConfigurationName(
        ILogger logger,
        string configurationName);

    /// <summary>
    /// Logs when configuration section is not found.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="configurationName">The name of the configuration section that was not found.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61003,
        Level = LogLevel.Warning,
        Message = "Configuration section not found: Authentication:{configurationName}")]
    public static partial IGenericMessage ConfigurationSectionNotFound(
        ILogger logger,
        string configurationName);

    /// <summary>
    /// Logs when AuthenticationType is not specified in configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="configurationName">The name of the configuration with missing AuthenticationType.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61004,
        Level = LogLevel.Warning,
        Message = "AuthenticationType not specified in configuration section: {configurationName}")]
    public static partial IGenericMessage AuthenticationTypeNotSpecified(
        ILogger logger,
        string configurationName);

    /// <summary>
    /// Logs when unknown authentication type is found in configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="authenticationType">The unknown authentication type from configuration.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 61005,
        Level = LogLevel.Warning,
        Message = "Unknown authentication type in configuration: {authenticationType}")]
    public static partial IGenericMessage UnknownAuthenticationTypeInConfiguration(
        ILogger logger,
        string authenticationType);

    /// <summary>
    /// Logs when configuration binding fails.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="configurationType">The type that configuration failed to bind to.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Failed to bind configuration section to type: {configurationType}")]
    public static partial IGenericMessage ConfigurationBindingFailed(
        ILogger logger,
        string? configurationType);

    /// <summary>
    /// Logs when getting authentication service by configuration name fails with exception.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="configurationName">The configuration name that failed.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Failed to get authentication service by configuration name: {configurationName}")]
    public static partial IGenericMessage GetAuthenticationServiceByNameException(
        ILogger logger,
        Exception exception,
        string configurationName);
}
