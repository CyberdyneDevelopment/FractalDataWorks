using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>What a data flow did in a run: the rows and batches that crossed each link, what each component did, and how long it took.</summary>
/// <param name="Links">The count of each link.</param>
/// <param name="Elapsed">How long the run took.</param>
/// <param name="Components">What each component did; empty for a report that was not measured per component.</param>
/// <param name="FailedComponent">The component whose failure stopped the run (FDW-1691); none when the run did not fail.</param>
/// <param name="Rejections">How many rows each error output carried and the reasons they gave, never the rows (FDW-1720); none for a run that rejected nothing.</param>
public sealed record FlowRunReport(
    IReadOnlyList<FlowLinkCount> Links,
    TimeSpan Elapsed,
    IReadOnlyList<FlowComponentReport> Components,
    string? FailedComponent = null,
    IReadOnlyList<FlowRejection>? Rejections = null)
{
    /// <summary>Initializes a report with no per-component measures.</summary>
    /// <param name="links">The count of each link.</param>
    /// <param name="elapsed">How long the run took.</param>
    public FlowRunReport(IReadOnlyList<FlowLinkCount> links, TimeSpan elapsed)
        : this(links, elapsed, [])
    {
    }
}
