using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Marker interface for health monitor service types (the options of <c>HealthMonitorTypes</c>).
/// </summary>
public interface IHealthMonitorType : IServiceType
{
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
