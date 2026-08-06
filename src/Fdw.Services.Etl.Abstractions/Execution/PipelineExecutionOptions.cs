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
    /// instead of executing the <c>BulkInsertCommand</c>.
    /// </summary>
    public bool SkipDestinationWrites { get; init; } = true;

    /// <summary>
    /// Gets the total in-memory byte budget for the sample ring buffers across all tasks and
    /// edges in a single test execution. Oldest samples are evicted when the budget is exceeded.
    /// 0 disables the cap (use with caution). Resolved from appsettings at startup.
    /// </summary>
    // Why: cap is configurable via PipelineTestMode:InspectorSampleBufferMaxBytes so
    // administrators can raise it for deeper inspection without redeploying code.
    public long SampleBufferMaxBytes { get; init; } = 10_000_000;

    /// <summary>
    /// Gets the maximum SignalR broadcast frequency in Hz for per-task and per-edge updates.
    /// Updates are coalesced — at most one message per <c>1000/BroadcastHz</c> ms. The final
    /// (terminal) broadcast is always sent regardless of cadence. Resolved from appsettings.
    /// </summary>
    public int BroadcastHz { get; init; } = 5;

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
