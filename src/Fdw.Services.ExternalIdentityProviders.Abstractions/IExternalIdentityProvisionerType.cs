using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Interface for external identity provisioner service types. Mirrors
/// <c>IExternalIdentityProviderType</c>'s generic/non-generic split. Like TokenManagers (and unlike
/// ExternalIdentityProviders), this domain resolves to exactly one active provisioner per (tenant,
/// external provider) selector via <c>ExternalIdentityProvisionerBindingConfigurationProvider</c> —
/// but multiple <c>ExternalIdentityProvisionerTypes</c> options (e.g. Chained plus, in the future, a
/// leaf provisioner) may still be simultaneously registered. Stays a pure marker (no domain-specific
/// capability properties).
/// </summary>
/// <typeparam name="TService">The external identity provisioner service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the external identity provisioner service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating external identity provisioner service instances.</typeparam>
public interface IExternalIdentityProvisionerType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, IExternalIdentityProvisionerType
    where TService : IExternalIdentityProvisioner
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IExternalIdentityProvisionerFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for external identity provisioner service types.
/// </summary>
public interface IExternalIdentityProvisionerType : IServiceType
{
    /// <summary>
    /// Registers this option's implementation provider factory with the domain provider being
    /// constructed, using the constructing scope's own <paramref name="serviceProvider"/>.
    /// </summary>
    /// <remarks>
    /// Called from <c>ExternalIdentityProvisionerTypes</c>'s own <c>AddScoped</c> factory — once
    /// per domain provider instance, not once at startup — so the closure this hands to the
    /// domain provider's <c>Register</c> always resolves against whichever scope actually
    /// constructed THIS instance (root at startup, a real request's own scope for a real
    /// request), never a reference captured from a different one. The prior approach registered
    /// from each option's <c>Initialize</c>, which only ever runs once against the root
    /// container — so every scope but root's own found this domain's factory registry empty.
    /// </remarks>
    /// <param name="domainProvider">The domain provider instance being constructed.</param>
    /// <param name="serviceProvider">The service provider that constructed it — root at startup, a request's own scope for a request.</param>
    /// <param name="logger">The logger to report the outcome to.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult RegisterImplementationProvider(IExternalIdentityProvisionerServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger);
}
