using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Stops a run on the ETL host for the StopRun escalation (FDW-3865): the run is the one this host holds, and it ends the way a cancelled run does (FDW-3784), with the
/// code <c>ThresholdStopped</c> and the threshold's reason as its message.
/// </summary>
public sealed class ThresholdRunStopper : IRunStopper
{
    private readonly PipelineRunControl _control;

    /// <summary>Initializes a new instance of the <see cref="ThresholdRunStopper"/> class.</summary>
    /// <param name="control">What the host holds of each run it has queued.</param>
    public ThresholdRunStopper(PipelineRunControl control)
    {
        _control = control;
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Stop(Guid runId, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(_control.StopForThreshold(runId, reason));
}
