using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The Local implementation of the HealthMonitor domain.
/// </summary>
/// <remarks>
/// Why a per-option interface, the same reason <c>ILocalHealthMonitorFactory</c> has one: each
/// implementation closes its base with its OWN interface, and that is what the domain provider
/// registers against.
/// </remarks>
public interface ILocalHealthMonitorProvider
    : IImplementationServiceProvider<IHealthMonitorService, IHealthMonitorImplementationConfiguration>
{
}
