using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers;

/// <summary>
/// Default provider for ETL row mappers.
/// Acts as a factory registry for creating mapper instances.
/// </summary>
public sealed class EtlRowMapperProvider : IEtlRowMapperProvider
{
    private readonly Dictionary<string, IEtlRowMapperFactory> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<EtlRowMapperProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlRowMapperProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public EtlRowMapperProvider(ILogger<EtlRowMapperProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string DefaultMapperType => "Pooled";

    /// <inheritdoc />
    public void Register(string serviceOptionType, IEtlRowMapperFactory factory)
    {
        if (string.IsNullOrWhiteSpace(serviceOptionType))
            throw new ArgumentNullException(nameof(serviceOptionType));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _factories[serviceOptionType] = factory;
        EtlRowMapperLog.MapperTypeRegistered(_logger, serviceOptionType, factory.GetType().Name);
    }

    /// <inheritdoc />
    public IGenericResult<IEtlRowMapper> Create(EtlRowMapperConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IEtlRowMapper>.Failure(
                EtlRowMapperLog.MapperCreationFailed(_logger, "null", "Configuration is null"));
        }

        var mapperType = string.IsNullOrWhiteSpace(configuration.MapperType)
            ? DefaultMapperType
            : configuration.MapperType;

        if (!_factories.TryGetValue(mapperType, out var factory))
        {
            return GenericResult<IEtlRowMapper>.Failure(
                EtlRowMapperLog.MapperCreationFailed(_logger, mapperType, $"Unknown mapper type: {mapperType}"));
        }

        return factory.Create(configuration);
    }

    /// <summary>
    /// Gets the number of registered mapper types.
    /// </summary>
    public int MapperTypeCount => _factories.Count;

    /// <summary>
    /// Completes provider initialization and logs the count.
    /// </summary>
    public void CompleteInitialization()
    {
        EtlRowMapperLog.ProviderInitialized(_logger, _factories.Count);
    }
}
