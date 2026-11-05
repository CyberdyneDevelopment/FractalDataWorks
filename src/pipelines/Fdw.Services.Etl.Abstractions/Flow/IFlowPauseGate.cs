using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The place a flow step waits while its run is paused (FDW-2950). A step awaits it between batches of rows, never in the middle of
/// one, so a pause holds the run with every batch it started finished.
/// </summary>
public interface IFlowPauseGate
{
    /// <summary>Completes at once while the run is not paused, and when the run is resumed.</summary>
    /// <param name="cancellationToken">Ends the wait when the run is cancelled.</param>
    /// <returns>A task that completes when the run may go on.</returns>
    Task WaitWhilePaused(CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether the run's token ended because the host is stopping and no person cancelled the run (FDW-3880). A flow that finds the run stopped this way writes what it did
    /// and leaves its records open: the host closes them Interrupted. A person's cancel and a timeout are not a host stop.
    /// </summary>
    bool StoppedWithHost { get; }
}
