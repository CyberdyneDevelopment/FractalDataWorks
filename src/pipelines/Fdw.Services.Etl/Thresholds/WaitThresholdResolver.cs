using System;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Chooses the owner whose attached thresholds apply to a run (FDW-3161, FDW-3975): the schedule's level when it does not inherit, else the pipeline's, else the server's.
/// The mode of the owner chosen says whether its attached thresholds apply (Own) or none do (None).
/// </summary>
public static class WaitThresholdResolver
{
    /// <summary>Chooses the owner.</summary>
    /// <param name="server">What the server states; it has no level above it, so it does not inherit.</param>
    /// <param name="pipeline">What the run's pipeline states.</param>
    /// <param name="schedule">What the run's schedule states, or <see langword="null"/> for a run no schedule started.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The level of the owner chosen; a failure when the server inherits.</returns>
    public static IGenericResult<WaitThresholdLayer> Owner(WaitThresholdLayer server, WaitThresholdLayer pipeline, WaitThresholdLayer? schedule, ILogger logger)
    {
        if (server.Mode.InheritsFromParent)
        {
            return GenericResult<WaitThresholdLayer>.Failure(WaitThresholdLog.ServerInherits(logger, server.Mode.Name));
        }

        if (schedule is not null && !schedule.Mode.InheritsFromParent)
        {
            return GenericResult<WaitThresholdLayer>.Success(schedule);
        }

        return GenericResult<WaitThresholdLayer>.Success(pipeline.Mode.InheritsFromParent ? server : pipeline);
    }
}
