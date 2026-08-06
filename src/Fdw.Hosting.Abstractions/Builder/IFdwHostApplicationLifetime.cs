using System.Threading;

namespace Fdw.Hosting.Abstractions.Builder;

/// <summary>
/// Provides notifications for application lifetime events.
/// </summary>
public interface IFdwHostApplicationLifetime
{
    /// <summary>
    /// Gets a token that is triggered when the application host has fully started.
    /// </summary>
    CancellationToken ApplicationStarted { get; }

    /// <summary>
    /// Gets a token that is triggered when the application host is starting a graceful shutdown.
    /// </summary>
    CancellationToken ApplicationStopping { get; }

    /// <summary>
    /// Gets a token that is triggered when the application host has completed a graceful shutdown.
    /// </summary>
    CancellationToken ApplicationStopped { get; }

    /// <summary>
    /// Requests termination of the current application.
    /// </summary>
    void StopApplication();
}
