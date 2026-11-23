using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The pipeline events a host that serves no hub hands to the host that does in one request (FDW-5249): every event of one tick, in the
/// order they happened. Each carries what a single <see cref="PipelineEventRequest"/> carries.
/// </summary>
public sealed class PipelineEventBatchRequest
{
    /// <summary>Gets or sets the events, oldest first. The receiving host relays them in this order.</summary>
    public List<PipelineEventRequest> Events { get; set; } = [];
}
