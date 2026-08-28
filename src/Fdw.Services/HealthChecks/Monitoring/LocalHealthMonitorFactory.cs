using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Factory for the in-process <see cref="HealthMonitorService"/> — the "Local" option of the health
/// monitor domain, used by hosts that ARE the health source (e.g. the API host).
/// </summary>
/// <remarks>
/// Injects the registered <see cref="IHealthCheckable"/> services and the host
/// <see cref="IServiceProvider"/> — the latter because the <see cref="IHealthCheckable.CheckHealth"/>
/// contract itself takes an <see cref="IServiceProvider"/>; the factory passes it through, it never
/// service-locates its own dependencies.
/// </remarks>
public sealed class LocalHealthMonitorFactory : ILocalHealthMonitorFactory
{
    private readonly IEnumerable<IHealthCheckable> _healthCheckables;
    private readonly IServiceProvider _services;
    private readonly ILogger<LocalHealthMonitorFactory> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<IHealthMonitorService>> _instances =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalHealthMonitorFactory"/> class.
    /// </summary>
    public LocalHealthMonitorFactory(
        IEnumerable<IHealthCheckable> healthCheckables,
        IServiceProvider services,
        ILoggerFactory? loggerFactory = null)
    {
        _healthCheckables = healthCheckables ?? throw new ArgumentNullException(nameof(healthCheckables));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<LocalHealthMonitorFactory>()
            ?? NullLogger<LocalHealthMonitorFactory>.Instance;
    }

    /// <inheritdoc/>
    public IGenericResult<IHealthMonitorService> Create(LocalHealthMonitorConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var instance = _instances.GetOrAdd(
            configuration.Name,
            _ => new Lazy<IHealthMonitorService>(() =>
                new HealthMonitorService(
                    _healthCheckables,
                    _services,
                    configuration,
                    _loggerFactory?.CreateLogger<HealthMonitorService>()))).Value;
        return GenericResult<IHealthMonitorService>.Success(instance);
    }

    /// <inheritdoc/>
    public IGenericResult<IHealthMonitorService> Create(IGenericConfiguration configuration)
    {
        if (configuration is not HealthMonitorConfiguration typed)
        {
            return GenericResult<IHealthMonitorService>.Failure(
                HealthMonitorLog.FactoryConfigurationCastFailed(
                    _logger, nameof(LocalHealthMonitorFactory),
                    nameof(HealthMonitorConfiguration), configuration?.GetType().Name ?? "null"));
        }

        return Create(typed);
    }

    /// <inheritdoc/>
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();
        return result.Value is T typed
            ? GenericResult<T>.Success(typed)
            : GenericResult<T>.Failure(
                HealthMonitorLog.FactoryConfigurationCastFailed(
                    _logger, nameof(LocalHealthMonitorFactory), typeof(T).Name, result.Value!.GetType().Name));
    }

    /// <inheritdoc/>
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess
            ? GenericResult<IGenericService>.Success(result.Value!)
            : result.ToNewResult<IGenericService>();
    }
}
