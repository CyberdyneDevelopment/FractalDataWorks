using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Services.Etl.Tracking;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Words what a cancelled or timed-out run had done and where it was stopped (FDW-3784): the rows it read, wrote and rejected, and the stages that did not finish
/// with the components in them that did not.
/// </summary>
/// <remarks>
/// Why from the run's records: a person looking at a run that was stopped asks how far it got. The figures are the ones its stages recorded, so a figure the records
/// do not hold is left out of the message and is never written as zero.
/// </remarks>
internal static class CancelledRunMessage
{
    /// <summary>Words what the run had done after the reason it was stopped.</summary>
    /// <param name="reason">Why the run was stopped.</param>
    /// <param name="tracking">What the run's records say it did.</param>
    /// <returns>The reason, then the rows it moved, then where it was stopped.</returns>
    public static string Of(string reason, RunTracking tracking)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(tracking);

        var parts = new List<string> { reason };
        var rows = new List<string>();
        if (tracking.RowsRead is { } read)
        {
            rows.Add($"read {read}");
        }

        if (tracking.RowsWritten is { } written)
        {
            rows.Add($"written {written}");
        }

        if (tracking.RowsRejected is { } rejected)
        {
            rows.Add($"rejected {rejected}");
        }

        if (rows.Count > 0)
        {
            parts.Add($"Rows {string.Join(", ", rows)}");
        }

        var stopped = tracking.Stages
            .Where(stage => !string.Equals(stage.State, ExecutionStateTypes.Completed.Name, StringComparison.Ordinal))
            .Select(Stopped)
            .ToList();
        if (stopped.Count > 0)
        {
            parts.Add($"Stopped in {string.Join("; ", stopped)}");
        }

        return string.Join(". ", parts);
    }

    // The stage and the components in it that had not succeeded; a stage whose components all succeeded is named alone.
    private static string Stopped(StageTracking stage)
    {
        var unfinished = stage.Components.Where(component => component.Succeeded != true).Select(component => component.Name).ToList();
        return unfinished.Count == 0 ? stage.Name : $"{stage.Name} ({string.Join(", ", unfinished)})";
    }
}
