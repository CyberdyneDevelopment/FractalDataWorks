using System;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// Represents a strongly-typed service type definition with generic type parameters for key, service, configuration, and factory.
/// This interface extends the Enhanced Enums pattern to provide type-safe service registration and discovery.
/// Used for services that require specific factory implementations.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
/// <typeparam name="TService">The service interface type that this service type provides.</typeparam>
/// <typeparam name="TFactory">The factory type used to create instances of the service.</typeparam>
/// <typeparam name="TConfiguration">The configuration type required by the service.</typeparam>
public interface IServiceType<TKey, TService, TFactory, TConfiguration> : IServiceType<TKey, TService, TFactory>
    where TKey : IEquatable<TKey>
    where TService : IGenericService
    where TFactory : IServiceFactory<TService, TConfiguration>
    where TConfiguration : IGenericConfiguration
{
}

/// <summary>
/// Represents a strongly-typed service type definition with generic type parameters for key, service, and factory.
/// This interface provides type safety for services that use standard factory patterns.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
/// <typeparam name="TService">The service interface type that this service type provides.</typeparam>
/// <typeparam name="TFactory">The type of factory required for the service</typeparam>
public interface IServiceType<TKey, TService, TFactory> : IServiceType<TKey, TService>
    where TKey : IEquatable<TKey>
    where TService : IGenericService
    where TFactory : IServiceFactory<TService>
{
}

/// <summary>
/// Represents a strongly-typed service type definition with generic type parameters for key and service.
/// This interface provides basic type safety for service registration and discovery.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
/// <typeparam name="TService">The service interface type that this service type provides.</typeparam>
public interface IServiceType<TKey, TService> : IServiceType<TKey>
    where TKey : IEquatable<TKey>
    where TService : IGenericService
{
}

/// <summary>
/// Base interface for all service type definitions in the Fdw framework.
/// Provides the fundamental contract for service registration, discovery, and factory creation.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier. Must implement IEquatable for dictionary lookups.</typeparam>
public interface IServiceType<TKey> : ITypeOption<TKey, IServiceType<TKey>>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the service interface type that this service type provides.
    /// </summary>
    Type ServiceType { get; }

    /// <summary>
    /// Gets the factory type for creating service instances.
    /// </summary>
    Type FactoryType { get; }

    /// <summary>
    /// Gets the configuration type required by this service type.
    /// </summary>
    Type ConfigurationType { get; }
}

/// <summary>
/// Non-generic convenience interface for IServiceType that defaults to using Guid as the key type.
/// Defines the two-phase registration pattern used by ServiceTypeCollections.
/// </summary>
// Why it extends IServiceTypeRegistration: that interface declares the three phases and the row
// location, and lives in Fdw.Collections so ServiceTypeCollectionBase can sweep its options. This
// interface cannot move there itself — it depends on Fdw.Abstractions and Fdw.Configuration, which
// depend on Fdw.Collections, so the reference would invert.
public interface IServiceType : IServiceType<Guid>, IServiceTypeRegistration
{
    // Configure / Register / Initialize come from IServiceTypeRegistration. The setters below replace
    // the body each of those invokes. Nothing else belongs here: RegisterFactory is gone (a provider
    // fills its own registry), and so are the Invoke* wrappers that existed only to check a nullable
    // override field before falling through to virtual dispatch.

    /// <summary>Sets this option's Configure body.</summary>
    /// <param name="method">The replacement delegate.</param>
    void Configuration(Func<IHostApplicationBuilder, IHostApplicationBuilder> method);

    /// <summary>Sets this option's Register body.</summary>
    /// <param name="method">The replacement delegate.</param>
    void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, string, string, string, IHostApplicationBuilder> method);

    /// <summary>Sets this option's Initialize body.</summary>
    /// <param name="method">The replacement delegate.</param>
    void Initialization(Func<IHost, ILoggerFactory?, IHost> method);
}
