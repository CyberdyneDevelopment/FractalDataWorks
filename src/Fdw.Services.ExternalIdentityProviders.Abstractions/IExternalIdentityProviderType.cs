using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Interface for external identity provider service types. Mirrors <c>ITokenManagerType</c>'s
/// generic/non-generic split, but — unlike TokenManagers — this domain is NOT a "declared choice":
/// multiple <c>ExternalIdentityProviderTypes</c> options may be simultaneously active (e.g. an "Oidc"
/// configuration for tenant A alongside another for tenant B). Stays a pure marker (no domain-specific
/// capability properties).
/// </summary>
/// <typeparam name="TService">The external identity provider service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the external identity provider service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating external identity provider service instances.</typeparam>
public interface IExternalIdentityProviderType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, IExternalIdentityProviderType
    where TService : IExternalIdentityProvider
    where TConfiguration : IGenericConfiguration
    where TFactory : IExternalIdentityProviderFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for external identity provider service types.
/// </summary>
public interface IExternalIdentityProviderType : IServiceType
{
}
