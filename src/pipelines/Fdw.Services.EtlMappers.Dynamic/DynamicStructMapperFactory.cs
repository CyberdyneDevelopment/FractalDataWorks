using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Dynamic;

/// <summary>
/// Factory for creating dynamic struct mapper instances.
/// </summary>
public sealed class DynamicStructMapperFactory
    : IEtlRowMapperFactory<DynamicStructMapper, DynamicStructMapperConfiguration>,
      IEtlRowMapperFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DynamicStructMapperFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicStructMapperFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public DynamicStructMapperFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DynamicStructMapperFactory>();
    }

    /// <inheritdoc />
    public IGenericResult<DynamicStructMapper> Create(DynamicStructMapperConfiguration configuration)
    {
        var resolved = Resolve(configuration);
        if (resolved.IsFailure) return resolved.ToNewResult<DynamicStructMapper>();

        try
        {
            var mapper = new DynamicStructMapper(
                _loggerFactory.CreateLogger<DynamicStructMapper>(),
                resolved.Value!);

            return GenericResult<DynamicStructMapper>.Success(mapper);
        }
        catch (System.Exception ex)
        {
            return GenericResult<DynamicStructMapper>.Failure(
                EtlRowMapperLog.MapperCreationFailed(_logger, "Dynamic", ex.Message));
        }
    }

    /// <inheritdoc />
    IGenericResult<IEtlRowMapper> IEtlRowMapperFactory.Create(EtlRowMapperConfiguration configuration)
    {
        if (configuration is DynamicStructMapperConfiguration dynamicConfig)
        {
            var result = Create(dynamicConfig);
            return result.IsSuccess
                ? GenericResult<IEtlRowMapper>.Success(result.Value!)
                : result.ToNewResult<IEtlRowMapper>();
        }

        // A configuration of another mapper type: its own name and pooling values are kept, and what it cannot state for this type is the
        // type's declared value (FDW-3956).
        var defaultConfig = new DynamicStructMapperConfiguration
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

    // Each value the configuration leaves out takes the Dynamic type's declared value, or a host's override of it (FDW-3956).
    private IGenericResult<DynamicStructMapperConfiguration> Resolve(DynamicStructMapperConfiguration configuration)
    {
        // Why the type itself and not EtlRowMapperTypes.ByName: the option restricts itself to its own compilation, so the collection may not
        // hold it; its declared values and a host's override (kept by its type name) are the same either way.
        var type = new DynamicStructMapperType();
        var pooling = EtlRowMapperDefaults.Resolve(configuration.EnablePooling, type, nameof(configuration.EnablePooling), _logger);
        if (pooling.IsFailure) return pooling.ToNewResult<DynamicStructMapperConfiguration>();

        var poolSize = EtlRowMapperDefaults.Resolve(configuration.MaxPoolSize, type, nameof(configuration.MaxPoolSize), _logger);
        if (poolSize.IsFailure) return poolSize.ToNewResult<DynamicStructMapperConfiguration>();

        var compiled = EtlRowMapperDefaults.Resolve(configuration.UseCompiledExpressions, type, nameof(configuration.UseCompiledExpressions), _logger);
        if (compiled.IsFailure) return compiled.ToNewResult<DynamicStructMapperConfiguration>();

        var cached = EtlRowMapperDefaults.Resolve(configuration.CacheCompiledDelegates, type, nameof(configuration.CacheCompiledDelegates), _logger);
        if (cached.IsFailure) return cached.ToNewResult<DynamicStructMapperConfiguration>();

        return GenericResult<DynamicStructMapperConfiguration>.Success(new DynamicStructMapperConfiguration
        {
            Id = configuration.Id,
            Name = configuration.Name,
            EnablePooling = pooling.Value,
            MaxPoolSize = poolSize.Value,
            UseCompiledExpressions = compiled.Value,
            CacheCompiledDelegates = cached.Value,
        });
    }
}
