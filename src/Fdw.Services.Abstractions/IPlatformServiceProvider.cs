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
public interface IPlatformServiceProvider
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
public interface IPlatformServiceProvider<TService> : IPlatformServiceProvider
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
}

/// <summary>
/// Strongly-typed service provider interface with configuration type constraint and registration support.
/// Use this for providers that participate in the ServiceType registration pattern.
/// </summary>
/// <typeparam name="TService">The type of service this provider manages.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for this service domain.</typeparam>
public interface IPlatformServiceProvider<TService, TConfiguration> : IPlatformServiceProvider<TService>
    where TService : IGenericService
    where TConfiguration : IImplementationConfiguration
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
    /// The configuration provider's own closed type — inferred at the call site, never written.
    /// </typeparam>
    // Why the type parameter follows the CONFIGURATION and not the provider: every configuration
    // provider is a ImplementationConfigurationProviderBase<TConfig, TCommand>, which implements
    // IServiceConfigurationProvider<TConfig> closed over its CONCRETE class. That interface is
    // invariant by C# rule — Save takes TConfig and Get returns it — so a parameter typed
    // IServiceConfigurationProvider<TConfiguration> could never accept one, whatever TConfiguration
    // is renamed to. Binding TConcrete to the concrete class and carrying the relationship in the
    // constraint converts directly, with no adapter and no variance change. The previous exact-typed
    IGenericResult Register<TConcrete>(string serviceOptionType, IServiceConfigurationProvider<TConcrete> configurationProvider)
        where TConcrete : class, TConfiguration;

    /// <summary>
    /// Registers a parent configuration provider for direct name-to-type resolution.
    /// The parent provider holds ALL configurations across all service option types,
    /// enabling O(1) lookup by name to determine which factory to use via
    /// <see cref="IGenericConfiguration.ServiceOptionType"/>.
    /// </summary>
    /// <param name="domainConfigurationProvider">The parent configuration provider.</param>
    IGenericResult Register(IDomainConfigurationProvider<TConfiguration> domainConfigurationProvider);

}

/// <summary>
/// Full service provider interface with specific factory and configuration provider type constraints.
/// </summary>
/// <typeparam name="TService">The type of service this provider manages.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for this service domain.</typeparam>
/// <typeparam name="TFactory">The factory type for creating service instances.</typeparam>
/// <typeparam name="TConfigurationProvider">The configuration provider type.</typeparam>
public interface IPlatformServiceProvider<TService, TConfiguration, TFactory, TConfigurationProvider>
    : IPlatformServiceProvider<TService, TConfiguration>
    where TService : IGenericService
    where TConfiguration : IImplementationConfiguration
    where TFactory : IServiceFactory<TService>
    where TConfigurationProvider : IDomainConfigurationProvider<TConfiguration>
{
}
