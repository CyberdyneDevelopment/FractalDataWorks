using Fdw.Services.Abstractions.Health.Monitoring;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Factory contract for the in-process ("Local") health monitor option.
/// </summary>
/// <remarks>
/// Why a per-option interface: each ServiceTypeOption closes its base with its OWN factory
/// interface — here <c>LocalHealthMonitorType</c> closes over this one — which is what gives every
/// option a distinct auto-generated Id. Options sharing the domain factory interface in the
/// closure collide and the second one never registers.
/// </remarks>
public interface ILocalHealthMonitorFactory : IHealthMonitorFactory<IHealthMonitorService, HealthMonitorConfiguration>
{
}
