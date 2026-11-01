using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Told by the executor when a stage is about to start (FDW-2060), so whatever records the run can open the stage's record before any component in it
/// starts, and an action in the stage can be recorded under it.
/// </summary>
public interface IFlowStageObserver
{
    /// <summary>Notes that a stage is about to start: every component of a lower stage has finished. Told once for each stage that holds a component.</summary>
    /// <param name="stage">The stage number; 0 for the components that have none, which run first.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the stage is noted; the stage's components start after it.</returns>
    Task StageStarting(int stage, CancellationToken cancellationToken = default);
}
