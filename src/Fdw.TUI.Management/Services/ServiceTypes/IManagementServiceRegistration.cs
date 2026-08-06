using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Defines a management service registration that can register its services with DI.
/// Simpler pattern than full ServiceTypes for local TUI services.
/// </summary>
public interface IManagementServiceRegistration : ITypeOption<int, ManagementServiceRegistrationBase>
{
    /// <summary>
    /// Gets the service lifetime for this service.
    /// </summary>
    ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets the description of what this service provides.
    /// </summary>
    string ServiceDescription { get; }

    /// <summary>
    /// Registers this service and its dependencies with the DI container.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    void Register(IServiceCollection services);
}
