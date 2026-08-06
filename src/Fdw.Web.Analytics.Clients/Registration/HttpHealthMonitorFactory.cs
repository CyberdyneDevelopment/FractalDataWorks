using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;
using Fdw.Services.HealthChecks.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// Factory for <see cref="HttpHealthMonitorService"/> — the "HttpClient" option of the health
/// monitor domain.
/// </summary>
public sealed class HttpHealthMonitorFactory : IHttpHealthMonitorFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpHealthMonitorFactory> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>Initializes a new instance of the <see cref="HttpHealthMonitorFactory"/> class.</summary>
    public HttpHealthMonitorFactory(IHttpClientFactory httpClientFactory, ILoggerFactory? loggerFactory = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<HttpHealthMonitorFactory>()
            ?? NullLogger<HttpHealthMonitorFactory>.Instance;
    }

    /// <inheritdoc/>
    public IGenericResult<IHealthMonitorService> Create(HealthMonitorConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        return GenericResult<IHealthMonitorService>.Success(
            new HttpHealthMonitorService(
                _httpClientFactory,
                _loggerFactory?.CreateLogger<HttpHealthMonitorService>()));
    }

    /// <inheritdoc/>
    public IGenericResult<IHealthMonitorService> Create(IGenericConfiguration configuration)
    {
        if (configuration is not HealthMonitorConfiguration typed)
        {
            return GenericResult<IHealthMonitorService>.Failure(
                HealthMonitorLog.FactoryConfigurationCastFailed(
                    _logger, nameof(HttpHealthMonitorFactory),
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
                    _logger, nameof(HttpHealthMonitorFactory), typeof(T).Name, result.Value!.GetType().Name));
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
