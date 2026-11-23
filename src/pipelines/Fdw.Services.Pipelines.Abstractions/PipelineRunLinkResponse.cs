namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// What one link between two components of a run in flight has carried so far (FDW-3778). Counts only; no row values.
/// </summary>
public sealed class PipelineRunLinkResponse
{
    /// <summary>Gets or sets the name of the component the rows leave.</summary>
    public required string From { get; set; }

    /// <summary>Gets or sets the name of the component the rows arrive at.</summary>
    public required string To { get; set; }

    /// <summary>Gets or sets the rows the link has carried.</summary>
    public long Rows { get; set; }

    /// <summary>Gets or sets the estimated value bytes the link has carried so far (FDW-5232). Estimated from the values' types, not process memory.</summary>
    public long Bytes { get; set; }

    /// <summary>Gets or sets the batches the link has carried so far (FDW-5232).</summary>
    public long Batches { get; set; }

    /// <summary>
    /// Gets or sets the estimated value bytes waiting now in the input the link feeds, written and not yet read (FDW-5230, FDW-5232). Links that feed one input report
    /// that input's queue.
    /// </summary>
    public long QueuedBytes { get; set; }

    /// <summary>Gets or sets the batches waiting now in the input the link feeds (FDW-5232).</summary>
    public long QueuedBatches { get; set; }

    /// <summary>Gets or sets how long, in milliseconds, writers have waited on the link because the input it feeds was full (FDW-5232).</summary>
    public long BlockedMs { get; set; }
}
