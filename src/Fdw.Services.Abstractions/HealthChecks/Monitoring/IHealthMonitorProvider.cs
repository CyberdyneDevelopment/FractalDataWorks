using Fdw.ServiceTypes;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Domain provider for health monitor services. Consumers (health endpoints, dashboard components)
/// depend on THIS interface and resolve the host's configured monitor by name — never on a direct
/// <see cref="IHealthMonitorService"/> registration.
/// </summary>
public interface IHealthMonitorProvider : IPlatformServiceProvider<IHealthMonitorService, IHealthMonitorImplementationConfiguration>
{
}
