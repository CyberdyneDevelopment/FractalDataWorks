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

        if (configuration is ISchedulerImplementationConfiguration typed)
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
