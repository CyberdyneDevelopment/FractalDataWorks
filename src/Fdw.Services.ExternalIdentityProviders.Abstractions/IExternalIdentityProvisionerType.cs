using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

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
}
