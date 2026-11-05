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
        var resolved = Resolve(configuration);
        if (resolved.IsFailure) return resolved.ToNewResult<PooledDictionaryMapper>();

        try
        {
            var mapper = new PooledDictionaryMapper(
                _loggerFactory.CreateLogger<PooledDictionaryMapper>(),
                resolved.Value!);

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

        // A configuration of another mapper type: its own name and pooling values are kept, and what it cannot state for this type is the
        // type's declared value (FDW-3956).
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

    // Each value the configuration leaves out takes the Pooled type's declared value, or a host's override of it (FDW-3956).
    private IGenericResult<PooledDictionaryMapperConfiguration> Resolve(PooledDictionaryMapperConfiguration configuration)
    {
        // Why the type itself and not EtlRowMapperTypes.ByName: the option restricts itself to its own compilation, so the collection may not
        // hold it; its declared values and a host's override (kept by its type name) are the same either way.
        var type = new PooledDictionaryMapperType();
        var pooling = EtlRowMapperDefaults.Resolve(configuration.EnablePooling, type, nameof(configuration.EnablePooling), _logger);
        if (pooling.IsFailure) return pooling.ToNewResult<PooledDictionaryMapperConfiguration>();

        var poolSize = EtlRowMapperDefaults.Resolve(configuration.MaxPoolSize, type, nameof(configuration.MaxPoolSize), _logger);
        if (poolSize.IsFailure) return poolSize.ToNewResult<PooledDictionaryMapperConfiguration>();

        var dictionarySize = EtlRowMapperDefaults.Resolve(configuration.MaxDictionarySize, type, nameof(configuration.MaxDictionarySize), _logger);
        if (dictionarySize.IsFailure) return dictionarySize.ToNewResult<PooledDictionaryMapperConfiguration>();

        return GenericResult<PooledDictionaryMapperConfiguration>.Success(new PooledDictionaryMapperConfiguration
        {
            Id = configuration.Id,
            Name = configuration.Name,
            EnablePooling = pooling.Value,
            MaxPoolSize = poolSize.Value,
            MaxDictionarySize = dictionarySize.Value,
        });
    }
}
