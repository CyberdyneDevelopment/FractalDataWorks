using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.HealthChecks.Monitoring;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// Factory contract for the HTTP-proxy ("HttpClient") health monitor option.
/// </summary>
/// <remarks>
/// Why a per-option interface: each ServiceTypeOption closes its base with its OWN factory
/// interface (the canonical shape — <c>MsSqlConnectionType</c>/<c>IMsSqlConnectionFactory</c>),
/// which is what gives every option a distinct auto-generated Id. Options sharing the domain
/// factory interface in the closure collide and the second one never registers.
/// </remarks>
public interface IHttpHealthMonitorFactory : IHealthMonitorFactory<IHealthMonitorService, HealthMonitorConfiguration>
{
}
