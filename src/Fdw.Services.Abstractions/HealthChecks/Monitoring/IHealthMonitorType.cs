using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Marker interface for health monitor service types (the options of <c>HealthMonitorTypes</c>).
/// </summary>
public interface IHealthMonitorType : IServiceType
{
    /// <summary>
    /// Registers this option's implementation provider factory with the domain provider being
    /// constructed, using the constructing scope's own <paramref name="serviceProvider"/>.
    /// </summary>
    /// <remarks>
    /// Called from <c>HealthMonitorTypes</c>'s own <c>AddScoped</c> factory — once per domain
    /// provider instance, not once at startup — so the closure this hands to the domain
    /// provider's <c>Register</c> always resolves against whichever scope actually constructed
    /// THIS instance (root at startup, a real request's own scope for a real request), never a
    /// reference captured from a different one. The prior approach registered from each option's
    /// <c>Initialize</c>, which only ever runs once against the root container — so every scope
    /// but root's own found this domain's factory registry empty.
    /// </remarks>
    /// <param name="domainProvider">The domain provider instance being constructed.</param>
    /// <param name="serviceProvider">The service provider that constructed it — root at startup, a request's own scope for a request.</param>
    /// <param name="logger">The logger to report the outcome to.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult RegisterImplementationProvider(IHealthMonitorProvider domainProvider, IServiceProvider serviceProvider, ILogger logger);
}

/// <summary>
/// Strongly-typed health monitor service type contract.
/// </summary>
/// <typeparam name="TService">The health monitor service type this option provides.</typeparam>
/// <typeparam name="TConfiguration">The configuration type this option requires.</typeparam>
/// <typeparam name="TFactory">The factory type that creates the service.</typeparam>
public interface IHealthMonitorType<TService, TConfiguration, TFactory>
    : IServiceType<Guid, TService, TFactory, TConfiguration>, IHealthMonitorType
    where TService : IHealthMonitorService
    where TConfiguration : IGenericConfiguration
    where TFactory : IHealthMonitorFactory<TService, TConfiguration>
{
}
