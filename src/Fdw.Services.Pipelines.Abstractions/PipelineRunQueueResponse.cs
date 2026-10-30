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
}
