using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Generic interface for connections with typed configuration.
/// Extends the base connection interface with configuration-specific functionality.
/// </summary>
/// <typeparam name="TConfiguration">The type of configuration this connection requires.</typeparam>
/// <remarks>
/// Use this interface for connections that require specific configuration objects
/// beyond basic connection strings. The configuration provides type safety and
/// validation for connection parameters.
/// </remarks>
public interface IConnection<TConfiguration> : IGenericConnection
    where TConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets the configuration object used by this connection.
    /// </summary>
    /// <value>The configuration object for this connection.</value>
    /// <remarks>
    /// This property provides access to the typed configuration used to create
    /// and configure the connection. The configuration is immutable after
    /// connection creation.
    /// </remarks>
    TConfiguration Configuration { get; }

    /// <summary>
    /// Initializes the connection with the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration object for this connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous initialization operation.
    /// The result indicates whether initialization was successful.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection has already been initialized.</exception>
    /// <remarks>
    /// This method must be called before the connection can be used. It validates
    /// the configuration and prepares the connection for opening.
    /// </remarks>
    Task<IGenericResult> Initialize(TConfiguration configuration, CancellationToken cancellationToken = default);
}
