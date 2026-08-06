using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Abstractions;

/// <summary>
/// Generic factory interface for creating Service instances
/// </summary>
public interface IServiceFactory
{
    /// <summary>
    /// Creates a service instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to create.</typeparam>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService;

    /// <summary>
    /// Creates a service instance.
    /// </summary>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    IGenericResult<IGenericService> Create(IGenericConfiguration configuration);
}

/// <summary>
/// Generic factory interface for creating Service instances of a specific type.
/// Covariant in TService to allow factories of derived types to be used where base type factories are expected.
/// </summary>
/// <typeparam name="TService">The type of service this factory creates</typeparam>
public interface IServiceFactory<out TService> : IServiceFactory
{
    /// <summary>
    /// Creates a service instance of the specified type.
    /// </summary>
    /// <param name="configuration">The configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    new IGenericResult<TService> Create(IGenericConfiguration configuration);
}

/// <summary>
/// Generic factory interface for creating Service instances with specific service and configuration types.
/// Covariant in TService, contravariant in TConfiguration.
/// </summary>
/// <typeparam name="TService">The type of service this factory creates</typeparam>
/// <typeparam name="TConfiguration">The configuration type required by the service</typeparam>
public interface IServiceFactory<out TService, in TConfiguration> : IServiceFactory<TService>
    where TConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Creates a service instance using the strongly-typed configuration.
    /// </summary>
    /// <param name="configuration">The strongly-typed configuration for the service.</param>
    /// <returns>A result containing the created service or an error message.</returns>
    IGenericResult<TService> Create(TConfiguration configuration);
}
