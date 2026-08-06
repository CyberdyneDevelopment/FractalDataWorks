using System;
using Microsoft.Extensions.DependencyInjection;
using Fdw.ServiceTypes;

namespace Fdw.ServiceTypes;

/// <summary>
/// Base implementation of registration options for all service types.
/// Provides common registration configuration with appropriate defaults.
/// </summary>
public class RegistrationOptions
{
    /// <summary>Gets or sets the service type the registration is bound to.</summary>
    public Type RegisterAs { get; set; } = null!;

    /// <summary>Gets or sets the DI lifetime applied when the service is registered.</summary>
    public ServiceLifetime Lifetime { get; set; }

    /// <summary>Gets or sets whether this registration is the primary for its service type.</summary>
    public bool RegisterAsPrimary { get; set; } = true;

    /// <summary>Gets or sets the configuration section name used to bind options for this service.</summary>
    public string ConfigurationSection { get; set; } = string.Empty;

    /// <summary>Gets or sets the service types that must be registered before this one.</summary>
    public Type[] RequiredServices { get; set; } = [];

    /// <summary>Gets or sets the service-domain types this registration depends on.</summary>
    public Type[] RequiredDomains { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationOptions"/> class.
    /// </summary>
    /// <param name="lifetime">The service lifetime for DI container registration. Defaults to Scoped.</param>
    public RegistrationOptions(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        Lifetime = lifetime;
    }
}