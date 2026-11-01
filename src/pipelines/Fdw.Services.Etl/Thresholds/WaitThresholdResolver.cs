using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Decides which thresholds apply to a run (FDW-3161): those of its schedule if the schedule states any or states none, otherwise those of its pipeline if the
/// pipeline states any or states none, otherwise those of the server. States none is a value someone sets and means no alert at all for the run.
/// </summary>
public static class WaitThresholdResolver
{
    /// <summary>Finds the thresholds that apply to a run.</summary>
    /// <param name="server">What the server states; it states its own thresholds or none, as it has no level above it.</param>
    /// <param name="pipeline">What the run's pipeline states.</param>
    /// <param name="schedule">What the run's schedule states; none for a run no schedule started.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The thresholds that apply, empty for a level that states none; a failure naming the level when it states its thresholds wrongly.</returns>
    public static IGenericResult<IReadOnlyList<WaitThreshold>> Effective(
        WaitThresholdLayer server, WaitThresholdLayer pipeline, WaitThresholdLayer? schedule, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(logger);

        if (server.Mode.InheritsFromParent)
        {
            return GenericResult<IReadOnlyList<WaitThreshold>>.Failure(WaitThresholdLog.ServerInherits(logger, server.Mode.Name));
        }

        foreach (var layer in new WaitThresholdLayer?[] { schedule, pipeline })
        {
            if (layer is null)
            {
                continue;
            }

            if (layer.Mode.InheritsFromParent)
            {
                if (layer.Thresholds.Count > 0)
                {
                    return GenericResult<IReadOnlyList<WaitThreshold>>.Failure(WaitThresholdLog.InheritWithThresholds(logger, layer.Owner, layer.Thresholds.Count));
                }
            }
            else
            {
                return Stated(layer, logger);
            }
        }

        return Stated(server, logger);
    }

    // A level's own thresholds, checked against what its mode says: a level that states thresholds has some, one that states none lists none.
    private static IGenericResult<IReadOnlyList<WaitThreshold>> Stated(WaitThresholdLayer layer, ILogger logger)
    {
        if (layer.Mode.StatesThresholds)
        {
            return layer.Thresholds.Count == 0
                ? GenericResult<IReadOnlyList<WaitThreshold>>.Failure(WaitThresholdLog.OwnWithoutThresholds(logger, layer.Owner))
                : GenericResult<IReadOnlyList<WaitThreshold>>.Success(layer.Thresholds);
        }

        return layer.Thresholds.Count == 0
            ? GenericResult<IReadOnlyList<WaitThreshold>>.Success([])
            : GenericResult<IReadOnlyList<WaitThreshold>>.Failure(WaitThresholdLog.NoneWithThresholds(logger, layer.Owner, layer.Thresholds.Count));
    }
}
