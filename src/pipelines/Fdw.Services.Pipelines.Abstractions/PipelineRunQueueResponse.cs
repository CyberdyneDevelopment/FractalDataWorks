using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The pipeline runs in the ETL host's queue that the caller reaches (FDW-3097): the runs in flight and the runs waiting, in the order they will start.
/// </summary>
public class PipelineRunQueueResponse
{
    /// <summary>Gets or sets the runs in flight.</summary>
    public IList<PipelineRunQueueItemResponse> Running { get; set; } = [];

    /// <summary>Gets or sets the runs waiting for their turn, in arrival order.</summary>
    public IList<PipelineRunQueueItemResponse> Waiting { get; set; } = [];

    /// <summary>
    /// Gets or sets how often, in seconds, a page that follows the queue reads it again (the setting <c>LiveViewRefreshSeconds</c> that applies to the caller, FDW-3382), or
    /// <see langword="null"/> when the setting is not set or is not positive: nothing is assumed, and the page does not read again by itself.
    /// </summary>
    public int? RefreshSeconds { get; set; }
}
