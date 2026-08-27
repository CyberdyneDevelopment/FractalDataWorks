using Fdw.Configuration;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// One configured health monitor — the domain record, naming which monitor implementation it is and
/// holding that implementation's own configuration.
/// </summary>
public interface IHealthMonitorConfiguration
    : IPlatformServiceConfiguration<IHealthMonitorImplementationConfiguration>
{
}
