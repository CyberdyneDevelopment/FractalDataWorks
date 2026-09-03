using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Limits;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Factory for the data gateway this framework ships.
/// </summary>
/// <remarks>
/// Why a factory and not a provider holding a captured instance: <see cref="Build"/> builds a
/// brand new gateway on every call, so there is nothing to be captive. The factory itself is
/// registered singleton -- it holds only the other singleton-safe pieces a gateway is built from
/// -- and what it produces is never singleton, never shared, never held across an ask.
/// <see cref="MainDataGatewayProvider"/> is the thin thing that calls this.
/// </remarks>
internal sealed class DataGatewayFactory : IDataGatewayFactory
{
    private readonly IDataConnectionProvider _connectionProvider;
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly DataStoreConfigurationProvider _dataStoreConfigProvider;
    private readonly IFrameworkAuthorizationService? _authorizationService;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly DataGatewayResultCache? _cache;
    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;
    private readonly ConnectionConfigurationProvider? _connectionConfigProvider;
    private readonly IConnectionLimitResolver _limitResolver;
    private readonly ConnectionLimitCounterStore _counters;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DataGatewayFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataGatewayFactory"/> class.</summary>
    public DataGatewayFactory(
        IDataConnectionProvider connectionProvider,
        IDataSetConfigurationProvider dataSetProvider,
        DataStoreConfigurationProvider dataStoreConfigProvider,
        IConnectionLimitResolver limitResolver,
        ConnectionLimitCounterStore counters,
        IFrameworkAuthorizationService? authorizationService = null,
        IDataStoreProvider? dataStoreProvider = null,
        DataGatewayResultCache? cache = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null,
        ConnectionConfigurationProvider? connectionConfigProvider = null,
        ILoggerFactory? loggerFactory = null,
        ILogger<DataGatewayFactory>? logger = null)
    {
        _connectionProvider = connectionProvider;
        _dataSetProvider = dataSetProvider;
        _dataStoreConfigProvider = dataStoreConfigProvider;
        _limitResolver = limitResolver;
        _counters = counters;
        _authorizationService = authorizationService;
        _dataStoreProvider = dataStoreProvider;
        _cache = cache;
        _authenticationContextAccessor = authenticationContextAccessor;
        _connectionConfigProvider = connectionConfigProvider;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = logger ?? NullLogger<DataGatewayFactory>.Instance;
    }

    private DataGatewayService Build(MainDataGatewayConfiguration configuration)
        => new(
            _loggerFactory,
            _connectionProvider,
            _dataSetProvider,
            _dataStoreConfigProvider,
            _limitResolver,
            _counters,
            _authorizationService,
            _dataStoreProvider,
            _cache,
            configuration,
            _authenticationContextAccessor,
            _connectionConfigProvider);

    /// <inheritdoc />
    public IGenericResult<IGenericService> Create(IServiceConfiguration configuration)
        => Create((IGenericConfiguration)configuration);

    /// <inheritdoc />
    public IGenericResult<IGenericService> Create(IGenericConfiguration configuration)
        => configuration is MainDataGatewayConfiguration typed
            ? GenericResult<IGenericService>.Success(Build(typed))
            : GenericResult<IGenericService>.Failure(
                DataGatewayProviderLog.ConfigurationTypeMismatch(
                    _logger, configuration?.GetType().Name ?? "(null)"));

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<T>();
        }

        return result.Value is T typed
            ? GenericResult<T>.Success(typed)
            : result.ToNewResult<T>();
    }
}
