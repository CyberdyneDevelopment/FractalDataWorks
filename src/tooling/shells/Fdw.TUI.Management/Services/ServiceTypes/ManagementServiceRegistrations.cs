using Fdw.Collections;
using Fdw.Collections.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// TypeCollection of management service registrations.
/// Use ManagementServiceRegistrations.RegisterAll() to register all services with DI.
/// </summary>
[TypeCollection(typeof(ManagementServiceRegistrationBase), typeof(IManagementServiceRegistration), typeof(ManagementServiceRegistrations))]
public partial class ManagementServiceRegistrations : TypeCollectionBase<ManagementServiceRegistrationBase, IManagementServiceRegistration>
{
    /// <summary>
    /// Registers all management services with the DI container.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    public static void RegisterAll(IServiceCollection services)
    {
        foreach (var registration in All())
        {
            registration.Register(services);
        }
    }
}
