using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Interface for identity service types.
/// </summary>
/// <typeparam name="TService">The identity service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the identity service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating identity service instances.</typeparam>
public interface IIdentityServiceType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, IIdentityServiceType
    where TService : IIdentityService
    where TConfiguration : IGenericConfiguration
    where TFactory : IIdentityServiceFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for identity service types.
/// </summary>
/// <remarks>
/// A pure marker. Unlike a domain of interchangeable engines with differing capabilities, identity
/// options differ only in how they prove identity to the provider — a difference that lives in the
/// option's behavior and its typed configuration body, not in capability metadata a caller branches
/// on. A caller that branched on "which identity mechanism is this" would be re-deciding something
/// the configuration already decided.
/// </remarks>
public interface IIdentityServiceType : IServiceType
{
}
