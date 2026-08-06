using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;



/// <summary>
/// Generic interface for services that provide specific functionality.
/// Extends the base service interface with typed configuration support.
/// </summary>
/// <typeparam name="TCommand">The type of command this service utilizes.</typeparam>
/// <remarks>
/// Use this interface for services that require specific configuration objects
/// to function properly. The configuration should be provided via constructor.
/// </remarks>
public interface IGenericService<TCommand> : IGenericService
    where TCommand : IGenericCommand
{
    // Id, ServiceType, and IsAvailable are inherited from IGenericService base interface

    /// <summary>
    /// Executes a command using the service.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task containing the execution result.</returns>
    Task<IGenericResult> Execute(TCommand command);
}

/// <summary>
/// Generic interface for services with command and configuration support.
/// Extends the command service interface with typed configuration access.
/// </summary>
/// <typeparam name="TCommand">The type of command this service executes.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this service uses.</typeparam>
/// <remarks>
/// This interface provides services with both command execution capabilities
/// and access to strongly-typed configuration objects. It represents the contract
/// that most service base classes implement.
/// </remarks>
public interface IGenericService<TCommand, TConfiguration> : IGenericService<TCommand>
    where TCommand : IGenericCommand
    where TConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets the service name for display purposes.
    /// </summary>
    /// <value>A human-readable name for the service.</value>
    /// <remarks>
    /// This name is used in user interfaces and logging to identify the service.
    /// It's typically the class name or a descriptive identifier.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the configuration instance for this service.
    /// </summary>
    /// <value>The strongly-typed configuration object.</value>
    /// <remarks>
    /// Provides access to the service's configuration for validation,
    /// logging, and runtime behavior modification.
    /// </remarks>
    TConfiguration Configuration { get; }

    /// <summary>
    /// Executes a command with generic return type.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task containing the execution result.</returns>
    Task<IGenericResult<T>> Execute<T>(TCommand command);

    /// <summary>
    /// Executes a command with generic return type and cancellation support.
    /// </summary>
    /// <typeparam name="TOut">The expected return type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the execution result.</returns>
    Task<IGenericResult<TOut>> Execute<TOut>(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a command with cancellation support.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the execution result.</returns>
    Task<IGenericResult> Execute(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Generic interface for services with command, configuration, and service type support.
/// Extends the configuration service interface with service type identification.
/// </summary>
/// <typeparam name="TCommand">The type of command this service executes.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this service uses.</typeparam>
/// <typeparam name="TService">The concrete service type for identification purposes.</typeparam>
/// <remarks>
/// This interface provides the full service contract with command execution,
/// configuration access, and service type identification. It matches the pattern
/// used by ServiceBase and ConnectionServiceBase classes.
/// </remarks>
public interface IGenericService<TCommand, TConfiguration, TService> : IGenericService<TCommand, TConfiguration>
    where TCommand : IGenericCommand
    where TConfiguration : IGenericConfiguration
    where TService : class
{
    // No additional members - the TService is used for type identification and logging
    // The concrete type provides the implementation details
}
