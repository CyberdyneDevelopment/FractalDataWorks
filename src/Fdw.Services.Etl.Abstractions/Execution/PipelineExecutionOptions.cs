using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Options controlling a single pipeline execution, including test-mode behavior.
/// Defaults to production mode (no test caps, writes enabled, no sample retention).
/// </summary>
public sealed record PipelineExecutionOptions
{
    /// <summary>
    /// Gets whether this execution runs in test mode.
    /// Test mode caps row extraction, optionally skips destination writes, and retains samples.
    /// </summary>
    public bool IsTestMode { get; init; }

    /// <summary>
    /// Gets the maximum rows extracted from each source task in test mode.
    /// Ignored in production mode. Applied as an upper bound — the source's own page size is used
    /// if it is already smaller.
    /// </summary>
    public int MaxRowsPerSource { get; init; } = 100;

    /// <summary>
    /// Gets whether destination writes are skipped in test mode.
    /// When true, load steps log "would write N rows" via <c>EtlLog.TestModeWriteSkipped</c>
    /// instead of executing the <c>InsertCommand</c>.
    /// </summary>
    public bool SkipDestinationWrites { get; init; } = true;

    /// <summary>
    /// Gets the total in-memory byte budget for the sample ring buffers across all tasks and
    /// edges in a single test execution. Oldest samples are evicted when the budget is exceeded.
    /// 0 disables the cap (use with caution). Resolved from appsettings at startup.
    /// </summary>
    public long SampleBufferMaxBytes { get; init; } = 10_000_000;

    /// <summary>
    /// Gets the maximum SignalR broadcast frequency in Hz for per-task and per-edge updates.
    /// Updates are coalesced — at most one message per <c>1000/BroadcastHz</c> ms. The final
    /// (terminal) broadcast is always sent regardless of cadence. Resolved from appsettings.
    /// </summary>
    public int BroadcastHz { get; init; } = 5;

    /// <summary>
    /// Gets the run parameters of this execution, by name, typed as the pipeline declares them. Null when none were supplied.
    /// {{name}} references in filter expressions read them, and a supplied LastSuccessfulRun overrides the stored watermark.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Gets the tenant the run belongs to. The last-successful watermark is kept per pipeline per tenant, so a run with no
    /// tenant neither reads nor writes one.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Gets the identifier of the execution record this run is tracked under (the id <c>IExecutionTracker</c> gave the run when
    /// it was queued). A run that starts another pipeline links the new run to this one as its parent (FDW-1381); a run with no
    /// tracked execution has nothing to link to, so a StartPipeline stage in it is refused rather than started unlinked.
    /// </summary>
    public Guid? ExecutionId { get; init; }

    /// <summary>
    /// Gets the instant the run started (FDW-1982): the one the host stamped when it took the run off the queue, and the one it records as the
    /// pipeline's watermark after the run succeeds. A graph pipeline offers it as the read-only <c>RunStarted</c>; a run that is not started by the
    /// host's queue states none, and anything that names <c>RunStarted</c> then fails saying the run has no value for it.
    /// </summary>
    public DateTimeOffset? RunStarted { get; init; }

    /// <summary>
    /// Gets the roles the run acts with. A pipeline the run starts acts with the same roles, so starting a run never grants
    /// more than the starter holds (FDW-1161); a run with none stated cannot start another pipeline.
    /// </summary>
    public IReadOnlyList<string>? RunRoles { get; init; }

    /// <summary>Gets the gate the run's flow steps wait at while the run is paused; empty for a run nobody can pause.</summary>
    public Fdw.Services.Etl.Abstractions.Flow.IFlowPauseGate? PauseGate { get; init; }

    /// <summary>
    /// Gets the production-mode defaults (no test mode, no sample retention).
    /// </summary>
    public static PipelineExecutionOptions Production { get; } = new();

    /// <summary>
    /// Gets a default test-mode options instance with standard caps.
    /// </summary>
    public static PipelineExecutionOptions DefaultTestMode { get; } = new()
    {
        IsTestMode = true,
        MaxRowsPerSource = 100,
        SkipDestinationWrites = true,
        SampleBufferMaxBytes = 10_000_000,
        BroadcastHz = 5
    };
}
