using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>The runs an ETL host takes up again when it starts (FDW-3216).</summary>
/// <param name="Requests">The requests of the runs that were accepted and not started, oldest first, a held run with the time it is held until.</param>
/// <param name="Approvals">The requests of the runs that were waiting for approval, oldest first; they go back into the approval holds and start when they are approved.</param>
/// <param name="Interrupted">The runs that were running when the host stopped (FDW-3236), oldest first, each with the number of times it has been started again.</param>
/// <param name="WithoutRequest">How many unstarted records have no request that can be taken up: none recorded (made before requests were recorded), which are left as they are, or one that cannot be read, which is closed as failed.</param>
public sealed record PipelineRunRecovery(IReadOnlyList<PipelineExecutionRequest> Requests, IReadOnlyList<PipelineExecutionRequest> Approvals, IReadOnlyList<InterruptedRun> Interrupted, int WithoutRequest);
