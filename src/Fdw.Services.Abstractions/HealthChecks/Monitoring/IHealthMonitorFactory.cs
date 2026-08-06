using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Non-generic marker for health monitor factories.
/// </summary>
public interface IHealthMonitorFactory
{
}

/// <summary>
/// Factory contract for creating <see cref="IHealthMonitorService"/> instances from a typed
/// configuration. One factory per registered <c>[ServiceTypeOption]</c> ("Local", "HttpClient", …);
/// the domain provider dispatches to the factory matching the configuration's
/// <c>ServiceOptionType</c>.
/// </summary>
/// <typeparam name="TService">The health monitor service type this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The configuration type this factory requires.</typeparam>
public interface IHealthMonitorFactory<TService, TConfiguration> : IHealthMonitorFactory, IServiceFactory<TService, TConfiguration>
    where TService : IHealthMonitorService
    where TConfiguration : IGenericConfiguration
{
}
