using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Counts a pipeline's consecutive failures over its history (FDW-3864): reads pages of the newest runs until the count is decided, that is, a success or the boundary ended it, or
/// the runs ran out.
/// </summary>
public static class PipelineFailureHistory
{
    // How many runs one read takes. A batch size for the reads, not a rule: the thresholds the pipeline states decide what the count means.
    private const int PageSize = 20;

    /// <summary>Counts the failures.</summary>
    /// <param name="history">Reads the pipeline's runs.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="notBefore">Runs that ended before this are not looked at; null looks at every run.</param>
    /// <param name="countTimedOut">Whether a run that timed out counts as a failure (FDW-4278).</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>The count; the failure when the history cannot be read.</returns>
    public static async Task<IGenericResult<PipelineFailureCount>> Count(
        IPipelineRunHistory history, string pipelineName, DateTimeOffset? notBefore, bool countTimedOut, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        var runs = new List<PipelineRunOutcome>();
        var skip = 0;
        while (true)
        {
            var page = await history.Page(pipelineName, skip, PageSize, cancellationToken).ConfigureAwait(false);
            if (page.IsFailure || page.Value is null)
            {
                return page.ToNewResult<PipelineFailureCount>();
            }

            runs.AddRange(page.Value);
            var count = PipelineFailureCounter.Count(runs, notBefore, countTimedOut);
            if (count.Ended || page.Value.Count == 0)
            {
                return GenericResult<PipelineFailureCount>.Success(count);
            }

            skip += PageSize;
        }
    }
}
