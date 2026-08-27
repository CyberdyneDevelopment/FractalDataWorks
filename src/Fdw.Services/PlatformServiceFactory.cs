using System;
using System.Globalization;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FastGenericNew;
using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Results;
using Fdw.Configuration;
using Fdw.Services.Logging;

namespace Fdw.Services;

/// <summary>
/// The one factory base. A service gets a named, closed subclass of it — <c>MsSqlConnectionFactory</c>,
/// <c>EmailNotificationFactory</c> — which overrides the behaviour it needs and inherits the rest.
/// </summary>
/// <typeparam name="TService">The type of service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The configuration type required by the service.</typeparam>
public abstract class PlatformServiceFactory<TService, TConfiguration> : IServiceFactory<TService, TConfiguration> where TService : class
    where TConfiguration : class, IGenericConfiguration
{
    private readonly ILogger<PlatformServiceFactory<TService, TConfiguration>> _logger;
    private readonly ILogger<TService> _serviceLogger;

    /// <summary>
    /// Gets the factory's own logger, for derived factories to narrate their creation logic.
    /// </summary>
    protected ILogger<PlatformServiceFactory<TService, TConfiguration>> Logger => _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformServiceFactory{TService,TConfiguration}"/> class.
    /// </summary>
    /// <param name="logger">The factory's own logger. A derived factory passes its own
    /// <c>ILogger&lt;TDerived&gt;</c>, which the covariant <c>ILogger&lt;out T&gt;</c> accepts here.</param>
    /// <param name="serviceLogger">The logger handed to each service this factory constructs.</param>
    /// <remarks>
    /// Two categories, deliberately: the factory narrates under its own name while the service it
    /// builds logs under <typeparamref name="TService"/>. Sharing one would file every service's
    /// output under whichever factory happened to construct it.
    /// </remarks>
    protected PlatformServiceFactory(
        ILogger<PlatformServiceFactory<TService, TConfiguration>>? logger,
        ILogger<TService>? serviceLogger)
    {
        _logger = logger ?? NullLogger<PlatformServiceFactory<TService, TConfiguration>>.Instance;
        _serviceLogger = serviceLogger ?? NullLogger<TService>.Instance;
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

        if (configuration is null)
            return GenericResult<TService>.Failure(
                ServicesResultCodes.ByName("ConfigurationRequired"),
                ResultDetails.Create("ServiceType", serviceTypeName));

        ServiceFactoryLogger.CreatingService(_logger, serviceTypeName, configuration.Name ?? "unnamed");

        // FastNew requires the service's own logger as the first constructor parameter.
        if (FastNew.TryCreateInstance<TService, ILogger<TService>, TConfiguration>(_serviceLogger, configuration, out var service))
            return GenericResult<TService>.Success(service, ServiceFactoryLogger.ServiceCreatedWithFastNew(_logger, serviceTypeName));

        return GenericResult<TService>.Failure(
            ServicesResultCodes.ByName("NoSuitableConstructor"),
            ResultDetails.Create("ServiceType", serviceTypeName, "ConfigurationType", typeof(TConfiguration).Name));
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
            return GenericResult<TConfiguration>.Failure(
                ServicesResultCodes.ByName("ConfigurationRequired"),
                ResultDetails.Create("ServiceType", typeof(TService).Name));
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
            ServicesResultCodes.ByName("InvalidConfigurationType"),
            ResultDetails.Create("ExpectedType", typeof(TConfiguration).Name, "ActualType", configuration.GetType().Name));
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
                ServicesResultCodes.ByName("ServiceCastFailed"),
                ResultDetails.Create("ExpectedType", typeof(TService).Name, "ActualType", typeof(T).Name));
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

        ServiceLogger.ServiceTypeCastFailed(_logger, sourceTypeName, targetTypeName);
        return GenericResult<T>.Failure(
            ServicesResultCodes.ByName("ServiceCastFailed"),
            ResultDetails.Create("ExpectedType", targetTypeName, "ActualType", sourceTypeName));
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

        ServiceLogger.ServiceTypeCastFailed(_logger, sourceTypeName, nameof(IGenericService));
        return GenericResult<IGenericService>.Failure(
            ServicesResultCodes.ByName("ServiceCastFailed"),
            ResultDetails.Create("ExpectedType", nameof(IGenericService), "ActualType", sourceTypeName));
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
