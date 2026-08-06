using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Hosting.Abstractions.Builder;

/// <summary>
/// Abstracts the host lifetime for managing startup and shutdown.
/// </summary>
public interface IFdwHostLifetime
{
    /// <summary>
    /// Called at the start of <see cref="IFdwHost.Start"/> to allow the host to start.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task WaitForStart(CancellationToken cancellationToken);

    /// <summary>
    /// Called from <see cref="IFdwHost.Stop"/> to indicate the host is stopping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task Stop(CancellationToken cancellationToken);
}
