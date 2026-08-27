using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Scheduling.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Default factory that produces <see cref="DefaultSchedulingService"/> instances from a
/// <see cref="SchedulerConfiguration"/>.
/// </summary>
// Why: SchedulerTypes is a ServiceTypeCollection; its provider expects an ISchedulingFactory<...>
// to construct services from configuration. DefaultSchedulingService takes a SchedulerConfiguration,
// IDataGateway, and optional ITenantContext. The factory wraps construction so the provider can hand
// it a configuration loaded from the gateway.
// Why: IHttpContextAccessor (not ITenantContext) is injected here. DefaultSchedulingFactory is
// registered as a singleton; ITenantContext is scoped. Capturing ITenantContext at construction
// time pins the root-scope context (HasTenant=false, TenantId=null) permanently, killing tenant
// filtering. IHttpContextAccessor is a singleton that reads the ambient per-request HttpContext,
// delivering the correct per-request ITenantContext at factory.Create() time — not at construction.
public sealed class DefaultSchedulingFactory : ISchedulingFactory<IFrameworkSchedulingService, ISchedulerImplementationConfiguration>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DefaultSchedulingFactory> _logger;
    private readonly IDataGateway _dataGateway;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="DefaultSchedulingFactory"/> class.</summary>
    public DefaultSchedulingFactory(
        ILoggerFactory loggerFactory,
        IDataGateway dataGateway,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger<DefaultSchedulingFactory>? logger = null)
    {
        _loggerFactory = loggerFactory;
        _logger = logger ?? NullLogger<DefaultSchedulingFactory>.Instance;
        _dataGateway = dataGateway;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public IGenericResult<IFrameworkSchedulingService> Create(ISchedulerImplementationConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IFrameworkSchedulingService>.Failure(
                SchedulingLog.FactoryConfigurationNull(_logger));

        // Why: Resolve ITenantContext from the current request's service scope, not from a
        // captured singleton-time context. The root scope's ITenantContext always has
        // HasTenant=false and TenantId=null, so schedule queries would never apply tenant
        // filtering. HttpContext.RequestServices provides the per-request scope.
        var tenantContext = _httpContextAccessor?.HttpContext?.RequestServices
            .GetService(typeof(ITenantContext)) as ITenantContext;

        var serviceLogger = _loggerFactory.CreateLogger<DefaultSchedulingService>();
        var service = new DefaultSchedulingService(serviceLogger, _dataGateway, configuration, tenantContext);
        SchedulingLog.FactoryServiceCreated(_logger, configuration.Name);
        return GenericResult<IFrameworkSchedulingService>.Success(service);
    }

    /// <inheritdoc />
    public IGenericResult<IFrameworkSchedulingService> Create(IGenericConfiguration configuration)
    {
        if (configuration is null)
            return GenericResult<IFrameworkSchedulingService>.Failure(
                SchedulingLog.FactoryConfigurationNull(_logger));

        if (configuration is SchedulerConfiguration typed)
            return Create(typed);

        return GenericResult<IFrameworkSchedulingService>.Failure(
            SchedulingLog.FactoryConfigurationTypeMismatch(_logger, configuration.GetType().FullName ?? "(unknown)"));
    }

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess)
            return result.ToNewResult<T>();

        if (result.Value is T typed)
            return GenericResult<T>.Success(typed);

        return GenericResult<T>.Failure(
            SchedulingLog.FactoryConfigurationTypeMismatch(_logger, typeof(T).FullName ?? "(unknown)"));
    }

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        return result.IsSuccess
            ? GenericResult<IGenericService>.Success(result.Value!)
            : result.ToNewResult<IGenericService>();
    }
}
