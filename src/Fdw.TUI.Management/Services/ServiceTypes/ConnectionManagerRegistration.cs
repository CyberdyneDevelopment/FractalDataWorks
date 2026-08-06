using Fdw.Collections.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Service registration for the connection manager.
/// </summary>
[TypeOption(typeof(ManagementServiceRegistrations), "ConnectionManager", RestrictToCurrentCompilation = true)]
public sealed class ConnectionManagerRegistration : ManagementServiceRegistrationBase
{
    /// <summary>
    /// Creates the connection manager registration.
    /// </summary>
    public ConnectionManagerRegistration() : base(
        id: 1,
        name: "ConnectionManager",
        serviceDescription: "Manages connections to Fdw instances",
        lifetime: ServiceLifetime.Singleton)
    {
    }

    /// <inheritdoc />
    public override void Register(IServiceCollection services)
    {
        services.AddSingleton<IConnectionManager, ConnectionManager>();
    }
}
