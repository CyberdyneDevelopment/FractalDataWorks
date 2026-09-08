using Fdw.Services.Abstractions.Health.Monitoring;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Resolves configured health monitors and routes each to the implementation provider that owns it.
/// </summary>
public interface IHealthMonitorConfigurationProvider
    : IDomainConfigurationProvider<IHealthMonitorImplementationConfiguration>
{
}
