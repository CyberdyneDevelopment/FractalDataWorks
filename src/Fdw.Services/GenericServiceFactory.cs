using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;


using Fdw.Services.Logging;
using FastGenericNew;
using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services;

/// <summary>
/// Generic factory implementation that works for most services.
/// Uses FastGenericNew for high-performance instantiation and follows Railway-Oriented Programming.
/// </summary>
/// <typeparam name="TService">The service type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the service.</typeparam>
public class GenericServiceFactory<TService, TConfiguration> : ServiceFactory<TService, TConfiguration>
    where TService : class, IGenericService
    where TConfiguration : class, IGenericConfiguration
{
    private readonly ILogger<TService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericServiceFactory{TService,TConfiguration}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public GenericServiceFactory(ILogger<TService> logger)
        : base(logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericServiceFactory{TService,TConfiguration}"/> class with no logger.
    /// </summary>
    public GenericServiceFactory()
        : base(null)
    {
        _logger = NullLogger<TService>.Instance;
    }

    /// <summary>
    /// Creates a service instance using the provided configuration.
    /// Follows ROP pattern - returns Result instead of throwing exceptions.
    /// </summary>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the service instance or failure information.</returns>
    public override IGenericResult<TService> Create(TConfiguration configuration)
    {
        var serviceTypeName = typeof(TService).Name;

        if (configuration is null)
            return GenericResult<TService>.Failure(ServiceLogger.ConfigurationCannotBeNull(_logger, serviceTypeName));

        // Log the creation attempt with source-generated logging
        ServiceFactoryLogger.CreatingService(_logger, serviceTypeName, configuration.Name ?? "unnamed");

        // Get instance using FastGenericNew - must pass logger as first parameter
        var serviceLogger = NullLogger<TService>.Instance; // FUTURE: Get proper logger for service
        if (FastNew.TryCreateInstance<TService, ILogger<TService>, TConfiguration>(serviceLogger, configuration, out var service))
        {
            return GenericResult<TService>.Success(service, ServiceFactoryLogger.ServiceCreatedWithFastNew(_logger, serviceTypeName));
        }

        // FastGenericNew failed - service creation fails
        return GenericResult<TService>.Failure(ServiceFactoryLogger.ServiceCreationFailed(_logger, serviceTypeName, "No suitable constructor found"));
    }

}