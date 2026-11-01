using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Logging;

/// <summary>
/// Static logger class for service factory operations.
/// </summary>
[MessageLoggingTypeCode("SERVICES")]
public static partial class ServiceFactoryLogger
{
    /// <summary>
    /// Logs the initiation of service creation with specified configuration.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="serviceType">The type of service being created.</param>
    /// <param name="configurationName">The name of the configuration being used.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Creating service '{serviceType}' with configuration '{configurationName}'")]
    public static partial IGenericMessage CreatingService(
        ILogger logger,
        string serviceType,
        string configurationName);

    /// <summary>
    /// Logs configuration validation failures for a service.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="serviceType">The type of service that failed validation.</param>
    /// <param name="validationErrors">The validation error details.</param>
    /// <returns>A generic message containing the warning information.</returns>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Configuration validation failed for service '{serviceType}': {validationErrors}")]
    public static partial IGenericMessage ConfigurationValidationFailed(
        ILogger logger,
        string serviceType,
        string validationErrors);

    /// <summary>
    /// Logs successful service creation using FastGenericNew optimization.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="serviceType">The type of service that was created.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Service '{serviceType}' created successfully using FastGenericNew")]
    public static partial IGenericMessage ServiceCreatedWithFastNew(
        ILogger logger,
        string serviceType);

    /// <summary>
    /// Logs service creation failures with error details.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="serviceType">The type of service that failed to be created.</param>
    /// <param name="reason">The reason for the creation failure.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Failed to create service '{serviceType}': {reason}")]
    public static partial IGenericMessage ServiceCreationFailed(
        ILogger logger,
        string serviceType,
        string reason);

    /// <summary>
    /// Logs successful retrieval of a factory from the dependency injection container.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="factoryType">The type of factory that was retrieved.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Debug,
        Message = "Factory '{factoryType}' retrieved from DI container")]
    public static partial IGenericMessage FactoryRetrievedFromContainer(
        ILogger logger,
        string factoryType);

    /// <summary>
    /// Logs the creation of a new factory instance.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="factoryType">The type of factory being created.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Debug,
        Message = "Creating new factory instance '{factoryType}'")]
    public static partial IGenericMessage CreatingFactoryInstance(
        ILogger logger,
        string factoryType);

    /// <summary>
    /// Logs when a service creation error occurs.
    /// </summary>
    /// <param name="logger">The logger instance to write to.</param>
    /// <param name="exception">The exception that occurred during service creation.</param>
    /// <param name="serviceType">The type of service that failed to be created.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Failed to create service of type '{serviceType}'")]
    public static partial IGenericMessage CreateServiceError(
        ILogger logger,
        Exception exception,
        string serviceType);
}
