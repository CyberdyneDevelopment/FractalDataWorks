using Fdw.Configuration;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// The configuration a health monitor is resolved against.
/// </summary>
/// <remarks>
/// It lives here rather than beside its class because a contract in this package cannot name a type in
/// the core package; the dependency runs the other way. Declaring it is what lets the domain's provider
/// contract name its configuration at all.
/// <para>
/// Health monitoring has no <c>Fdw.Services.HealthMonitor</c> package of its own — it sits inside
/// <c>Fdw.Services</c> and <c>Fdw.Services.Abstractions</c> — so this interface joins its siblings
/// <see cref="IHealthMonitorFactory{TService, TConfiguration}"/> and <see cref="IHealthMonitorType"/>
/// here rather than in a domain package that does not exist.
/// </para>
/// </remarks>
public interface IHealthMonitorImplementationConfiguration : IImplementationConfiguration
{
}
