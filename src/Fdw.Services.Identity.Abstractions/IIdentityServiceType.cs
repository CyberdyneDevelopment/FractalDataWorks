using System;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

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
    /// <summary>
    /// Registers this option's implementation provider factory with the domain provider being
    /// constructed, using the constructing scope's own <paramref name="serviceProvider"/>.
    /// </summary>
    /// <remarks>
    /// Called from <c>IdentityServiceTypes</c>'s own <c>AddScoped</c> factory — once per domain
    /// provider instance, not once at startup — so the closure this hands to the domain
    /// provider's <c>Register</c> always resolves against whichever scope actually constructed
    /// THIS instance (root at startup, a real request's own scope for a real request), never a
    /// reference captured from a different one. The prior approach registered from each option's
    /// <c>Initialize</c>, which only ever runs once against the root container — so every scope
    /// but root's own found this domain's factory registry empty.
    /// </remarks>
    /// <param name="domainProvider">The domain provider instance being constructed.</param>
    /// <param name="serviceProvider">The service provider that constructed it — root at startup, a request's own scope for a request.</param>
    /// <param name="logger">The logger to report the outcome to.</param>
    /// <returns>Success, or a structured failure. The base no-op returns success for options with nothing to contribute.</returns>
    IGenericResult RegisterImplementationProvider(IIdentityServiceProvider domainProvider, IServiceProvider serviceProvider, ILogger logger);
}
