using Fdw.Configuration;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.ServiceTypes;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Base class for health monitor service type definitions (the options of <see cref="HealthMonitorTypes"/>).
/// </summary>
/// <typeparam name="TService">The health monitor service type this option provides.</typeparam>
/// <typeparam name="TFactory">The factory type that creates the service.</typeparam>
/// <typeparam name="TConfiguration">The configuration type this option requires.</typeparam>
/// <remarks>
/// Options wire their factory into <see cref="HealthMonitorProvider"/> in
/// <c>RegisterFactory</c>; consumers depend on <see cref="IHealthMonitorProvider"/> and resolve by the
/// host's configured row name — never on a direct <see cref="IHealthMonitorService"/> registration
/// (a direct registration is the registration-order race this domain exists to eliminate).
/// </remarks>
public abstract class HealthMonitorTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IHealthMonitorType<TService, TConfiguration, TFactory>
    where TService : IHealthMonitorService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IHealthMonitorFactory<TService, TConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorTypeBase{TService,TFactory,TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The name of this health monitor type (matches a configuration row's <c>ServiceOptionType</c>).</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="displayName">The display name for this service type.</param>
    /// <param name="description">The description of what this service type provides.</param>
    protected HealthMonitorTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description)
        : base(name, sectionName, displayName, description, category: "HealthMonitor",
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "settings",
               defaultContainerName: "HealthMonitor")
    {
    }
}
