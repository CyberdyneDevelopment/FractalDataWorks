using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Fdw.Hosting.Abstractions.Builder;

/// <summary>
/// Represents a built FDW host that can be started and stopped.
/// </summary>
public interface IFdwHost : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the service provider for the host.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the configuration for the host.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// Starts the host.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task Start(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the host.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task Stop(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the host until it is stopped.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the host stops.</returns>
    Task Run(CancellationToken cancellationToken = default);
}
