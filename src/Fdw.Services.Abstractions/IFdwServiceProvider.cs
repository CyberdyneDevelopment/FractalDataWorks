using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.ServiceTypes;

/// <summary>
/// Base interface for Fdw service providers.
/// Use when you need to get any service without knowing the specific TService type.
/// </summary>
public interface IFdwServiceProvider
{
    /// <summary>Gets a service instance by configuration name.</summary>
    Task<IGenericResult<T>> Get<T>(string name, CancellationToken cancellationToken = default) where T : IGenericService;

    /// <summary>Gets a service instance by configuration ID.</summary>
    Task<IGenericResult<T>> Get<T>(Guid id, CancellationToken cancellationToken = default) where T : IGenericService;

    /// <summary>Gets all service instances.</summary>
    Task<IGenericResult<IReadOnlyList<T>>> Get<T>(CancellationToken cancellationToken = default) where T : IGenericService;
}

/// <summary>
/// Strongly-typed service provider interface.
/// </summary>
/// <typeparam name="TService">The type of service this provider manages.</typeparam>
public interface IFdwServiceProvider<TService> : IFdwServiceProvider
    where TService : IGenericService
{
    /// <summary>Gets a service instance by configuration name.</summary>
    Task<IGenericResult<TService>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a service instance by configuration ID.</summary>
    Task<IGenericResult<TService>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all service instances.</summary>
    Task<IGenericResult<IReadOnlyList<TService>>> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a service instance built from the supplied configuration. No name/id lookup is
    /// performed — the configuration's <see cref="IGenericConfiguration.ServiceOptionType"/>
    /// selects the factory directly. A null configuration or missing ServiceOptionType is a
    /// structured failure, never a fallback.
    /// </summary>
    Task<IGenericResult<TService>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>Evicts a cached service instance by name so the next Get() recreates it.</summary>
    void Evict(string name);

    /// <summary>Evicts a cached service instance by ID so the next Get() recreates it.</summary>
    void Evict(Guid id);
}

/// <summary>
/// Strongly-typed service provider interface with configuration type constraint and registration support.
/// Use this for providers that participate in the ServiceType registration pattern.
/// </summary>
/// <typeparam name="TService">The type of service this provider manages.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for this service domain.</typeparam>
public interface IFdwServiceProvider<TService, TConfiguration> : IFdwServiceProvider<TService>
    where TService : IGenericService
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Gets a service instance built from the supplied strongly-typed configuration.
    /// No name/id lookup is performed — the configuration's
    /// <see cref="IGenericConfiguration.ServiceOptionType"/> selects the factory directly.
    /// </summary>
    Task<IGenericResult<TService>> Get(TConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a factory for a service option type.
    /// </summary>
    IGenericResult Register(string serviceOptionType, IServiceFactory<TService> factory);

    /// <summary>
    /// Registers a configuration provider for a service option type.
    /// </summary>
    // Why exact-typed only: this serves domains whose child config type IS the domain's configuration
    // type (Scheduling, ExternalIdentityProviders). The widening overload that used to sit beside it —
    // Register{TDerived} for a SUBTYPE — existed solely to bridge the invariant
    // IServiceConfigurationProvider{T} via a forwarding adapter, and a typed body belongs on the
    // domain's HEADER provider by discriminator, not here.
    IGenericResult Register(string serviceOptionType, IServiceConfigurationProvider<TConfiguration> configurationProvider);

    /// <summary>
    /// Registers a parent configuration provider for direct name-to-type resolution.
    /// The parent provider holds ALL configurations across all service option types,
    /// enabling O(1) lookup by name to determine which factory to use via
    /// <see cref="IGenericConfiguration.ServiceOptionType"/>.
    /// </summary>
    /// <param name="parentProvider">The parent configuration provider.</param>
    IGenericResult Register(IServiceConfigurationProvider<TConfiguration> parentProvider);

}

/// <summary>
/// Full service provider interface with specific factory and configuration provider type constraints.
/// </summary>
/// <typeparam name="TService">The type of service this provider manages.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for this service domain.</typeparam>
/// <typeparam name="TFactory">The factory type for creating service instances.</typeparam>
/// <typeparam name="TConfigurationProvider">The configuration provider type.</typeparam>
public interface IFdwServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    : IFdwServiceProvider<TService, TConfiguration>
    where TService : IGenericService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IServiceConfigurationProvider<TConfiguration>
{
}
