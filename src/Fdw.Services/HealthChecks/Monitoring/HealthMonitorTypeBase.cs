using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

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
    /// <param name="name">The name of this health monitor type (matches a configuration row's <c>Implementation</c>).</param>
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

    // ── Domain-provider/domain-configuration-provider registration ─────────────────────────────
    // An overload of Registration/Register, not a new phase and not a new method name. Stored and
    // invoked exactly like the DI-wiring Registration(Func<IHostApplicationBuilder, ...>) overload
    // already inherited from ServiceTypeBase -- this is simply a second signature of the same verb,
    // distinguished by its parameter types, the way every other framework-wide "the verb, overloaded"
    // convention already works in this codebase.

    private Func<IServiceProvider, IHealthMonitorProvider?, IHealthMonitorConfigurationProvider?, ILogger<IHealthMonitorType>, IGenericResult> _domainRegistrationMethod
        = static (_, _, _, _) => GenericResult.Success();

    /// <inheritdoc/>
    public void Registration(Func<IServiceProvider, IHealthMonitorProvider?, IHealthMonitorConfigurationProvider?, ILogger<IHealthMonitorType>, IGenericResult> method)
    {
        _domainRegistrationMethod = method;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Base default: an option that never called the overload above (none exist yet, but the base
    /// must not force every option to set one) reports success and leaves both providers untouched.
    /// </remarks>
    public IGenericResult Register(IServiceProvider serviceProvider, IHealthMonitorProvider? domainProvider, IHealthMonitorConfigurationProvider? domainConfigurationProvider, ILogger<IHealthMonitorType> logger)
        => _domainRegistrationMethod(serviceProvider, domainProvider, domainConfigurationProvider, logger);
}
