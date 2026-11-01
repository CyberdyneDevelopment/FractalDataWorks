using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Pooled;

/// <summary>
/// Factory for creating pooled dictionary mapper instances.
/// </summary>
public sealed class PooledDictionaryMapperFactory
    : IEtlRowMapperFactory<PooledDictionaryMapper, PooledDictionaryMapperConfiguration>,
      IEtlRowMapperFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PooledDictionaryMapperFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledDictionaryMapperFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public PooledDictionaryMapperFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<PooledDictionaryMapperFactory>();
    }

    /// <inheritdoc />
    public IGenericResult<PooledDictionaryMapper> Create(PooledDictionaryMapperConfiguration configuration)
    {
        try
        {
            var mapper = new PooledDictionaryMapper(
                _loggerFactory.CreateLogger<PooledDictionaryMapper>(),
                configuration);

            return GenericResult<PooledDictionaryMapper>.Success(mapper);
        }
        catch (System.Exception ex)
        {
            return GenericResult<PooledDictionaryMapper>.Failure(
                EtlRowMapperLog.MapperCreationFailed(_logger, "Pooled", ex.Message));
        }
    }

    /// <inheritdoc />
    IGenericResult<IEtlRowMapper> IEtlRowMapperFactory.Create(EtlRowMapperConfiguration configuration)
    {
        if (configuration is PooledDictionaryMapperConfiguration pooledConfig)
        {
            var result = Create(pooledConfig);
            return result.IsSuccess
                ? GenericResult<IEtlRowMapper>.Success(result.Value!)
                : result.ToNewResult<IEtlRowMapper>();
        }

        // Create with default configuration
        var defaultConfig = new PooledDictionaryMapperConfiguration
        {
            Name = configuration.Name,
            EnablePooling = configuration.EnablePooling,
            MaxPoolSize = configuration.MaxPoolSize
        };

        var defaultResult = Create(defaultConfig);
        return defaultResult.IsSuccess
            ? GenericResult<IEtlRowMapper>.Success(defaultResult.Value!)
            : defaultResult.ToNewResult<IEtlRowMapper>();
    }
}
