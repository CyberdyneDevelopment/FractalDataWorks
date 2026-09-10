using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>Supplies this implementation's own configuration to the domain that registers it.</summary>
public interface ILocalHealthMonitorConfigurationProvider
    : IImplementationConfigurationProvider<IHealthMonitorImplementationConfiguration>
{
}
