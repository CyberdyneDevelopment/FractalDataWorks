using System;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What moved over one link in a run (FDW-1386): the row count SSIS shows on a path.
/// </summary>
/// <param name="Link">The link, or for a dropped output the link whose target is the empty string.</param>
/// <param name="Rows">The number of rows written to the link.</param>
/// <param name="Batches">The number of batches written to the link.</param>
/// <param name="Bytes">The estimated size of the rows written to the link (<see cref="RowSize"/>): an estimate of the memory their values hold, not a size on disk or on the wire.</param>
public sealed record FlowLinkCount(FlowLink Link, long Rows, long Batches, long Bytes)
{
    /// <summary>
    /// Gets the estimated value bytes (<see cref="RowSize"/>) waiting now in the input the link feeds, written and not yet read (FDW-5230). Links that feed one input
    /// report that input's queue.
    /// </summary>
    public long QueuedBytes { get; init; }

    /// <summary>Gets the batches waiting now in the input the link feeds, written and not yet read (FDW-5230).</summary>
    public long QueuedBatches { get; init; }

    /// <summary>Gets how long writers have waited on this link so far because the input it feeds was full (FDW-5230).</summary>
    public TimeSpan Blocked { get; init; }
}
