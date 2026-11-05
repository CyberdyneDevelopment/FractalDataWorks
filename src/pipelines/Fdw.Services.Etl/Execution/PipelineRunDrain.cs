using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Waits for the runs in flight to finish, up to a stated time, then stops the rest (FDW-3786).
/// </summary>
/// <remarks>
/// Why the stop is a callback: the drain knows nothing of how a run is cancelled; the host gives it the one action that cancels every run still
/// running, and the runs then end through their own stop path, which marks each Interrupted. Why it waits for the runs after stopping them: each
/// run closes its records on its way out, and the host must not exit before they are closed (the host's shutdown timeout bounds the wait).
/// </remarks>
internal static class PipelineRunDrain
{
    /// <summary>Waits for the runs, then stops those that do not finish in the time.</summary>
    /// <param name="runs">The tasks of the runs the host started.</param>
    /// <param name="grace">How long the runs are given.</param>
    /// <param name="stopRuns">Stops every run still running.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>How many runs finished in the time and how many were stopped.</returns>
    public static async Task<PipelineRunDrained> Wait(IReadOnlyList<Task> runs, TimeSpan grace, Action stopRuns, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(stopRuns);
        var log = logger ?? NullLogger.Instance;

        var inFlight = runs.Where(run => !run.IsCompleted).ToList();
        PipelineRunRecoveryLog.StopDrainStarted(log, inFlight.Count, grace.TotalSeconds);
        if (inFlight.Count == 0)
        {
            PipelineRunRecoveryLog.StopDrainFinished(log, 0, 0);
            return new PipelineRunDrained(0, 0);
        }

        var all = Task.WhenAll(inFlight);
        using var timer = new CancellationTokenSource();
        var first = await Task.WhenAny(all, Task.Delay(grace, timer.Token)).ConfigureAwait(false);
        await timer.CancelAsync().ConfigureAwait(false);
        if (ReferenceEquals(first, all))
        {
            _ = await Task.WhenAny(all).ConfigureAwait(false);
            PipelineRunRecoveryLog.StopDrainFinished(log, inFlight.Count, 0);
            return new PipelineRunDrained(inFlight.Count, 0);
        }

        var stopped = inFlight.Count(run => !run.IsCompleted);
        stopRuns();
        _ = await Task.WhenAny(all).ConfigureAwait(false);
        PipelineRunRecoveryLog.StopDrainFinished(log, inFlight.Count - stopped, stopped);
        return new PipelineRunDrained(inFlight.Count - stopped, stopped);
    }
}
