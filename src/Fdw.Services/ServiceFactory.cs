using System;
using System.Globalization;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FastGenericNew;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Logging;

namespace Fdw.Services;

/// <summary>
/// Base implementation of the service factory with comprehensive type-safe creation patterns.
/// Provides a complete foundation for service factories with automatic configuration validation,
/// type checking, and structured logging support.
/// </summary>
/// <typeparam name="TService">The type of service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The configuration type required by the service.</typeparam>
public abstract class ServiceFactory<TService, TConfiguration> : IServiceFactory<TService, TConfiguration> where TService : class
    where TConfiguration : class, IGenericConfiguration
{
    private readonly ILogger<TService> _logger;

    /// <summary>
    /// Gets the logger instance for derived classes.
    /// </summary>
    protected ILogger<TService> Logger => _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFactory{TService,TConfiguration}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance. If null, uses Microsoft's NullLogger.</param>
    protected ServiceFactory(ILogger<TService>? logger)
    {
        // Use Microsoft's NullLogger for consistency with ILogger abstractions
        // This works seamlessly when Serilog is registered via services.AddSerilog()
        _logger = logger ?? NullLogger<TService>.Instance;
    }

    /// <summary>
    /// Creates a service instance for the specified configuration.
    /// Uses FastGeneric for high-performance instantiation.
    /// </summary>
    /// <param name="configuration">The configuration to use for service creation.</param>
    /// <returns>A result containing the created service or failure message.</returns>
    public virtual IGenericResult<TService> Create(TConfiguration configuration)
    {
        var serviceTypeName = typeof(TService).Name;

        if (configuration == null)
        {
            return GenericResult<TService>.Failure(ServiceLogger.InvalidConfigurationWarning(_logger, "Configuration cannot be null"));
        }

        // Log configuration
        ServiceLogger.ValidatingServiceConfiguration(_logger, serviceTypeName);

        // Must pass logger as first parameter per service constructor pattern
        var serviceLogger = NullLogger<TService>.Instance; // FUTURE: Get proper logger for service
        if (FastNew.TryCreateInstance<TService, ILogger<TService>, TConfiguration>(serviceLogger, configuration, out var service))
        {
            // Use structured logging for success
            ServiceLogger.FastGenericServiceCreated(_logger, serviceTypeName);
            ServiceLogger.ServiceStarted(_logger, serviceTypeName);

            // Use Enhanced Enum factory method with parameters
            return GenericResult<TService>.Success(service, $"Service created successfully: {serviceTypeName}");
        }

        // Use structured logging and Enhanced Enum factory method with parameters for failure
        ServiceLogger.FastGenericServiceCreationFailed(_logger, serviceTypeName);
        var exception = new InvalidOperationException("FastNew failed to create service");

        return GenericResult<TService>.Failure(ServiceFactoryLogger.CreateServiceError(_logger, exception, serviceTypeName));
    }


    #region Configuration Validation

    /// <summary>
    /// Validates and casts a configuration to the expected type.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="validConfiguration">The valid configuration if successful.</param>
    /// <returns>The validation result.</returns>
    protected IGenericResult<TConfiguration> ValidateConfiguration(
        IGenericConfiguration? configuration,
        out TConfiguration? validConfiguration)
    {
        if (configuration == null)
        {
            validConfiguration = null;
            return GenericResult<TConfiguration>.Failure(ServiceLogger.InvalidConfigurationWarning(_logger, "Configuration cannot be null"));
        }

        if (configuration is TConfiguration config)
        {
            validConfiguration = config;
            return GenericResult<TConfiguration>.Success(config);
        }

        var errorMessage = string.Format(CultureInfo.InvariantCulture,
            "Invalid configuration type. Expected {0}, got {1}",
            typeof(TConfiguration).Name,
            configuration.GetType().Name);

        validConfiguration = null;
        return GenericResult<TConfiguration>.Failure(
            ServiceLogger.InvalidConfigurationWarning(_logger, errorMessage));
    }

    #endregion

    #region IServiceFactory Implementation (Non-Generic)

    /// <summary>
    /// Creates a service instance of the specified type.
    /// This method checks if the requested type matches the factory's service type.
    /// </summary>
    /// <typeparam name="T">The type of service to create.</typeparam>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        // Check if the requested type is assignable from our service type
        if (!typeof(T).IsAssignableFrom(typeof(TService)))
        {
            var errorMessage = string.Format(CultureInfo.InvariantCulture,
                "Invalid service type. Expected {0} or compatible type, got {1}",
                typeof(TService).Name,
                typeof(T).Name);

            return GenericResult<T>.Failure(
                ServiceLogger.InvalidConfigurationWarning(_logger, errorMessage));
        }

        // Validate configuration and create service
        var validationResult = ValidateConfiguration(configuration, out var validConfig);
        if (validationResult.Error || validConfig == null)
        {
            return validationResult.ToNewResult<T>();
        }

        var serviceResult = Create(validConfig);
        if (serviceResult.Error || serviceResult.Value == null)
        {
            return serviceResult.ToNewResult<T>();
        }

        if (serviceResult.Value is T typedService)
        {
            return serviceResult.ToNewResult(typedService);
        }

        // Use structured logging and Enhanced Enum factory method with parameters
        var sourceTypeName = typeof(TService).Name;
        var targetTypeName = typeof(T).Name;

        return GenericResult<T>.Failure(ServiceLogger.ServiceTypeCastFailed(_logger, sourceTypeName, targetTypeName));
    }

    /// <summary>
    /// Creates a service instance and returns it as IFractalService.
    /// </summary>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        // Validate configuration and create service
        var validationResult = ValidateConfiguration(configuration, out var validConfig);
        if (validationResult.Error || validConfig == null)
        {
            return validationResult.ToNewResult<IGenericService>();
        }

        var serviceResult = Create(validConfig);
        if (serviceResult.Error || serviceResult.Value == null)
        {
            return serviceResult.ToNewResult<IGenericService>();
        }

        if (serviceResult.Value is IGenericService recService)
        {
            return serviceResult.ToNewResult(recService);
        }

        // Use structured logging and Enhanced Enum factory method with parameters
        var sourceTypeName = typeof(TService).Name;

        return GenericResult<IGenericService>.Failure(ServiceLogger.ServiceTypeCastFailed(_logger, sourceTypeName, nameof(IGenericService)));
    }

    #endregion

    #region IServiceFactory<TService> Implementation

    /// <summary>
    /// Creates a service instance with configuration validation.
    /// This method validates that the configuration is of the correct type before creation.
    /// </summary>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    IGenericResult<TService> IServiceFactory<TService>.Create(IGenericConfiguration configuration)
    {
        // Validate configuration and create service
        var validationResult = ValidateConfiguration(configuration, out var validConfig);
        if (validationResult.Error || validConfig == null)
        {
            return validationResult.ToNewResult<TService>();
        }

        return Create(validConfig);
    }

    #endregion

}
