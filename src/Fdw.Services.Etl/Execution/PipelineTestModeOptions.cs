namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Appsettings POCO bound from the <c>PipelineTestMode</c> configuration section.
/// Provides server-level defaults for test execution caps and broadcast frequency.
/// </summary>
/// <remarks>
/// <code>
/// "PipelineTestMode": {
///   "SampleBufferMaxBytes": 10000000,
///   "BroadcastHz": 5,
///   "MaxRowsPerSource": 100
/// }
/// </code>
/// </remarks>
public sealed class PipelineTestModeOptions
{
    /// <summary>
    /// Gets or sets the total byte budget for inspector sample ring buffers per test execution.
    /// Oldest samples are evicted when the budget is exceeded.
    /// 0 disables the cap. Default: 10 MB.
    /// </summary>
    public long InspectorSampleBufferMaxBytes { get; set; } = 10_000_000;

    /// <summary>
    /// Gets or sets the SignalR broadcast frequency for per-task and per-edge counter updates.
    /// Updates are coalesced; at most one message is sent per <c>1000/BroadcastHz</c> ms.
    /// The final terminal broadcast is always sent. Default: 5 Hz.
    /// </summary>
    public int BroadcastHz { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum rows extracted from each source per test execution.
    /// Acts as an upper bound — the source's own page size is applied if already smaller.
    /// Default: 100.
    /// </summary>
    public int MaxRowsPerSource { get; set; } = 100;
}
