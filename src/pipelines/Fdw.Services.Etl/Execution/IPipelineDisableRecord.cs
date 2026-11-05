using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Execution;

/// <summary>The record of why the failure rule disabled a pipeline (FDW-3864): written by the judge, read by the graph read.</summary>
public interface IPipelineDisableRecord
{
    /// <summary>Records the count, why the rule fired and the failures that counted, on the run whose end made it fire.</summary>
    /// <param name="runId">The run.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="consecutiveFailures">How many failures in a row counted.</param>
    /// <param name="reason">Why the rule fired.</param>
    /// <param name="failures">The failures that counted, newest first.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<IGenericResult> Write(Guid runId, string pipelineName, int consecutiveFailures, string reason, IReadOnlyList<PipelineRunOutcome> failures, CancellationToken cancellationToken = default);

    /// <summary>Reads what was recorded on the run.</summary>
    /// <param name="runId">The run.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The evidence; a failure when the run holds no such record or it cannot be read.</returns>
    Task<IGenericResult<PipelineDisableEvidence>> Read(Guid runId, CancellationToken cancellationToken = default);
}
