using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services;

/// <summary>
/// Base class for all services in the Fdw framework.
/// Provides common implementation for service lifecycle management and command execution.
/// </summary>
/// <typeparam name="TCommand">The type of command this service executes.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this service uses.</typeparam>
/// <typeparam name="TService">The concrete service type for identification purposes.</typeparam>
/// <remarks>
/// This class implements IGenericService and provides default Execute implementations
/// that use pattern matching to convert IGenericCommand to TCommand. Derived classes
/// implement protected abstract methods that accept the strongly-typed TCommand.
/// </remarks>
public abstract class ServiceBase<TCommand, TConfiguration, TService> : IGenericService, IDisposable
    where TCommand : IGenericCommand
    where TConfiguration : IGenericConfiguration
    where TService : class
{
    private readonly ILogger<TService> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the unique identifier for this service instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the service.
    /// </summary>
    public string ServiceType { get; }

    /// <summary>
    /// Gets a value indicating whether the service is currently available for use.
    /// </summary>
    public virtual bool IsAvailable { get; protected set; } = true;

    /// <summary>
    /// Gets the service name for display purposes.
    /// </summary>
    public string Name => Configuration?.Name ?? typeof(TService).Name;

    /// <summary>
    /// Gets the configuration instance for this service.
    /// </summary>
    protected TConfiguration Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBase{TCommand, TConfiguration, TService}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this service.</param>
    /// <param name="configuration">The configuration for this service.</param>
    protected ServiceBase(ILogger<TService>? logger, TConfiguration configuration)
    {
        _logger = logger ?? NullLogger<TService>.Instance;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Id = Guid.NewGuid().ToString();
        ServiceType = typeof(TService).Name;
    }

    /// <summary>
    /// Executes a command using pattern matching to convert IGenericCommand to TCommand.
    /// This is the IGenericService.Execute implementation that provides runtime type checking.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The command to execute (IGenericCommand).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    public async Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        if (command is TCommand typedCommand)
        {
            return await Execute<T>(typedCommand, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<T>.Failure(
            ServiceBaseLogger.CommandTypeMismatch(Logger, typeof(TCommand).Name, command?.GetType().Name ?? "null"));
    }

    /// <summary>
    /// Executes a command using pattern matching to convert IGenericCommand to TCommand.
    /// This is the IGenericService.Execute implementation that provides runtime type checking.
    /// </summary>
    /// <param name="command">The command to execute (IGenericCommand).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    public async Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        if (command is TCommand typedCommand)
        {
            return await Execute(typedCommand, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult.Failure(
            ServiceBaseLogger.CommandTypeMismatch(Logger, typeof(TCommand).Name, command?.GetType().Name ?? "null"));
    }

    /// <summary>
    /// Executes a typed command with cancellation support.
    /// </summary>
    /// <param name="command">The typed command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    public abstract Task<IGenericResult> Execute(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a typed command with generic return type and cancellation support.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The typed command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    public abstract Task<IGenericResult<T>> Execute<T>(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the logger for derived classes to use.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the service. Override to add custom disposal logic.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    public virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Derived classes override this to dispose their resources
            }
            _disposed = true;
        }
    }
}
