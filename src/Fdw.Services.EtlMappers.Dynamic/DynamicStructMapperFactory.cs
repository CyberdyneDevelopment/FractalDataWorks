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
        try
        {
            var mapper = new DynamicStructMapper(
                _loggerFactory.CreateLogger<DynamicStructMapper>(),
                configuration);

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

        // Create with default configuration
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
}
