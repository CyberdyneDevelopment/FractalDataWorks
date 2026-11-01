namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// The in-process health monitor implementation's configuration.
/// </summary>
/// <remarks>
/// An implementation of the HealthMonitor domain, so the domain's provider can hand it back: a
/// provider returns <see cref="IHealthMonitorImplementationConfiguration"/>, and only a type
/// carrying it can be returned or registered against this domain.
/// </remarks>
public interface ILocalHealthMonitorConfiguration : IHealthMonitorImplementationConfiguration
{
}
