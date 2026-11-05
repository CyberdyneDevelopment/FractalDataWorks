using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Counts a pipeline's consecutive failures (FDW-3864): newest first, a failure that counts adds one, a run that does not count (a transient failure, a person's cancel, a
/// host stop) is passed over without adding and without ending the count, and a success ends it. A run that ended before the pipeline was last changed (a person editing or
/// enabling it again writes a new version) is not looked at, so a pipeline a person enabled again is not disabled by the failures that disabled it. What the count means is the
/// pipeline's thresholds' to say; this only measures it.
/// </summary>
public static class PipelineFailureCounter
{
    /// <summary>Counts the failures.</summary>
    /// <param name="newestFirst">The pipeline's runs, newest first.</param>
    /// <param name="notBefore">Runs that ended before this are not looked at; null looks at every run.</param>
    /// <returns>The count, the failures that counted, and whether a success or the boundary ended it.</returns>
    public static PipelineFailureCount Count(IReadOnlyList<PipelineRunOutcome> newestFirst, DateTimeOffset? notBefore)
    {
        ArgumentNullException.ThrowIfNull(newestFirst);

        var counted = new List<PipelineRunOutcome>();
        var ended = false;
        foreach (var run in newestFirst)
        {
            if ((notBefore is { } boundary && run.At < boundary) || run.Succeeded)
            {
                ended = true;
                break;
            }

            if (run.Counts)
            {
                counted.Add(run);
            }
        }

        return new PipelineFailureCount(counted.Count, counted, ended);
    }
}
