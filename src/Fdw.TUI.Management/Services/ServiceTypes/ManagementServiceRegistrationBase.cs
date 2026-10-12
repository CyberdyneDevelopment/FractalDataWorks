using Fdw.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Abstract base class for management service registrations.
/// Inherit from this class and apply [TypeOption] attribute to create service registrations.
/// </summary>
public abstract class ManagementServiceRegistrationBase : TypeOptionBase<ManagementServiceRegistrationBase>, IManagementServiceRegistration
{
    /// <summary>
    /// Creates a new management service registration.
    /// </summary>
    /// <param name="name">Unique name used for lookup.</param>
    /// <param name="serviceDescription">Description of what this service provides.</param>
    /// <param name="lifetime">The service lifetime (nullable for Empty sentinel support).</param>
    protected ManagementServiceRegistrationBase(
        string name,
        string serviceDescription,
        ServiceLifetime? lifetime = null)
        : base(name)
    {
        ServiceDescription = serviceDescription;
        Lifetime = lifetime ?? ServiceLifetime.Scoped;
    }

    /// <inheritdoc />
    public string ServiceDescription { get; }

    /// <inheritdoc />
    public ServiceLifetime Lifetime { get; }

    /// <inheritdoc />
    public abstract void Register(IServiceCollection services);
}
